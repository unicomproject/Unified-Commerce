using System.Text.Json;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Repositories;

public sealed class PosHardwareRepository : IPosHardwareRepository
{
    private readonly EPosDbContext _db;

    public PosHardwareRepository(EPosDbContext db) => _db = db;

    public async Task<IReadOnlyList<PosHardwareConfigurationDto>> GetConfigurationsAsync(
        Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        var activeTillId = await _db.TillDeviceAssignments.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PosDeviceId == posDeviceId &&
                        x.ReleasedAt == null)
            .Select(x => (Guid?)x.TillId)
            .FirstOrDefaultAsync(cancellationToken);
        var activeSession = await ActiveSessionAsync(
            tenantId, posDeviceId, activeTillId, cancellationToken);
        var rows = await (
            from assignment in _db.HardwareDeviceAssignments.AsNoTracking()
            join device in _db.HardwareDevices.AsNoTracking()
                on assignment.HardwareDeviceId equals device.Id
            where assignment.TenantId == tenantId &&
                  assignment.PosDeviceId == posDeviceId &&
                  assignment.ReleasedAt == null &&
                  device.Status != "DELETED"
            orderby device.HardwareDeviceType
            select new { assignment, device }).ToListAsync(cancellationToken);

        return rows.Select(x => MapConfiguration(
            x.device, posDeviceId, activeTillId,
            activeSession?.Id, activeSession is not null)).ToList();
    }

    public async Task<(string? ErrorCode, PosHardwareConfigurationDto? Configuration)> SaveConfigurationAsync(
        Guid tenantId,
        Guid userId,
        SavePosHardwareConfigurationRequest request,
        string safeSettingsJson,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var posDevice = await _db.PosDevices.SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == request.PosDeviceId,
            cancellationToken);
        if (posDevice is null || !posDevice.IsTrusted || posDevice.Status != "ACTIVE")
            return ("pos_hardware.device_not_trusted", null);
        if (posDevice.OutletId != request.OutletId)
            return ("pos_hardware.assignment_mismatch", null);

        if (request.TillId is { } tillId)
        {
            var assigned = await _db.TillDeviceAssignments.AsNoTracking().AnyAsync(
                x => x.TenantId == tenantId && x.OutletId == request.OutletId &&
                     x.TillId == tillId && x.PosDeviceId == request.PosDeviceId &&
                     x.ReleasedAt == null,
                cancellationToken);
            if (!assigned) return ("pos_hardware.assignment_mismatch", null);
        }

        var type = request.HardwareType.Trim().ToUpperInvariant();
        var activeSession = await ActiveSessionAsync(
            tenantId, request.PosDeviceId, request.TillId, cancellationToken);
        var existing = await (
            from assignment in _db.HardwareDeviceAssignments
            join device in _db.HardwareDevices on assignment.HardwareDeviceId equals device.Id
            where assignment.TenantId == tenantId &&
                  assignment.PosDeviceId == request.PosDeviceId &&
                  assignment.ReleasedAt == null &&
                  device.HardwareDeviceType == type &&
                  device.Status != "DELETED"
            select new { assignment, device }).SingleOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            if (request.ExpectedVersion != 0)
                return ("pos_hardware.version_conflict", null);

            var id = Guid.NewGuid();
            var code = $"HW-{request.PosDeviceId:N}-{type}";
            var device = HardwareDevice.Create(
                id, tenantId, request.OutletId, null, code,
                request.DisplayName, type, request.TransportType,
                null, null, null, null, null, safeSettingsJson,
                request.Enabled ? "ACTIVE" : "DISABLED", userId, now);
            var assignment = HardwareDeviceAssignment.Create(
                Guid.NewGuid(), tenantId, request.OutletId, id, null,
                request.PosDeviceId, true, userId, now);
            _db.HardwareDevices.Add(device);
            _db.HardwareDeviceAssignments.Add(assignment);
            _db.HardwareConfigurationChangeAudits.Add(
                HardwareConfigurationChangeAudit.Create(
                    Guid.NewGuid(), tenantId, request.OutletId, request.PosDeviceId,
                    id, request.TillId, activeSession?.Id, 0, 1, "CREATED",
                    request.ChangeReason, "{}", SafeAuditJson(request, safeSettingsJson),
                    userId, now));
            await _db.SaveChangesAsync(cancellationToken);
            return (null, MapConfiguration(
                device, request.PosDeviceId, request.TillId,
                activeSession?.Id, activeSession is not null));
        }

        if (existing.device.ConfigurationVersion != request.ExpectedVersion)
            return ("pos_hardware.version_conflict", null);

        var criticalChanged =
            !string.Equals(existing.device.ConnectionType, request.TransportType, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(existing.device.ConfigJson, safeSettingsJson, StringComparison.Ordinal) ||
            existing.device.Status != (request.Enabled ? "ACTIVE" : "DISABLED");
        if (activeSession is not null && criticalChanged && string.IsNullOrWhiteSpace(request.ChangeReason))
            return ("pos_hardware.active_shift_reason_required", null);

        var oldVersion = existing.device.ConfigurationVersion;
        var before = SafeAuditJson(existing.device);
        existing.device.UpdateConfiguration(
            request.DisplayName, request.TransportType, safeSettingsJson,
            request.Enabled ? "ACTIVE" : "DISABLED",
            request.ExpectedVersion, userId, now);
        _db.HardwareConfigurationChangeAudits.Add(
            HardwareConfigurationChangeAudit.Create(
                Guid.NewGuid(), tenantId, request.OutletId, request.PosDeviceId,
                existing.device.Id, request.TillId, activeSession?.Id,
                oldVersion, existing.device.ConfigurationVersion, "UPDATED",
                request.ChangeReason, before,
                SafeAuditJson(request, safeSettingsJson), userId, now));

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ("pos_hardware.version_conflict", null);
        }

        return (null, MapConfiguration(
            existing.device, request.PosDeviceId, request.TillId,
            activeSession?.Id, activeSession is not null));
    }

    public async Task<(string? ErrorCode, HardwareTestOperationDto? Operation)> CreateTestAsync(
        Guid tenantId,
        Guid userId,
        CreateHardwareTestRequest request,
        string requestPayloadHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await _db.HardwareTestLogs.SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.RequestId == request.RequestId,
            cancellationToken);
        if (existing is not null)
            return existing.RequestPayloadHash == requestPayloadHash
                ? (null, MapTest(existing))
                : ("pos_hardware.request_id_conflict", null);

        var device = await _db.PosDevices.AsNoTracking().SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == request.PosDeviceId,
            cancellationToken);
        if (device is null || !device.IsTrusted || device.Status != "ACTIVE")
            return ("pos_hardware.device_not_trusted", null);

        HardwareDevice? configuration = null;
        if (request.HardwareConfigurationId is { } configurationId)
        {
            configuration = await _db.HardwareDevices.AsNoTracking().SingleOrDefaultAsync(
                x => x.TenantId == tenantId && x.Id == configurationId &&
                     x.OutletId == device.OutletId && x.Status != "DELETED",
                cancellationToken);
            if (configuration is null)
                return ("pos_hardware.configuration_not_found", null);
            var assigned = await _db.HardwareDeviceAssignments.AsNoTracking()
                .AnyAsync(x =>
                    x.TenantId == tenantId &&
                    x.HardwareDeviceId == configurationId &&
                    x.PosDeviceId == request.PosDeviceId &&
                    x.ReleasedAt == null,
                    cancellationToken);
            if (!assigned)
                return ("pos_hardware.assignment_mismatch", null);
            if (!string.Equals(
                    ToCamel(configuration.HardwareDeviceType),
                    request.HardwareType,
                    StringComparison.OrdinalIgnoreCase))
                return ("pos_hardware.configuration_not_found", null);
            if (configuration.Status != "ACTIVE")
                return ("pos_hardware.scanner_disabled", null);
            if (configuration.ConfigurationVersion != request.ConfigurationVersion)
                return ("pos_hardware.version_conflict", null);
            if (request.HardwareType.Equals(
                    "barcodeScanner",
                    StringComparison.OrdinalIgnoreCase) &&
                !ScannerModeSupportsTest(configuration.ConfigJson, request.TestType))
                return ("pos_hardware.unsupported_scanner_mode", null);
        }

        if (request.TillId is { } tillId)
        {
            var tillAssigned = await _db.TillDeviceAssignments.AsNoTracking()
                .AnyAsync(x =>
                    x.TenantId == tenantId &&
                    x.OutletId == device.OutletId &&
                    x.TillId == tillId &&
                    x.PosDeviceId == request.PosDeviceId &&
                    x.ReleasedAt == null,
                    cancellationToken);
            if (!tillAssigned)
                return ("pos_hardware.assignment_mismatch", null);
        }

        var activeSession = await ActiveSessionAsync(
            tenantId, request.PosDeviceId, request.TillId, cancellationToken);
        var operation = HardwareTestLog.Create(
            Guid.NewGuid(), tenantId, device.OutletId, configuration?.Id,
            request.PosDeviceId, request.TillId, activeSession?.Id, userId,
            request.RequestId, requestPayloadHash, request.ConfigurationVersion,
            request.HardwareType, request.TestType, "PENDING", null, null, null, now, now);

        if (request.HardwareType.Equals("cardTerminal", StringComparison.OrdinalIgnoreCase))
            operation.Complete("BLOCKED", "CARD_TERMINAL_NOT_CONFIGURED",
                "Card terminal is blocked because no provider or device is configured.", null, false, now);

        _db.HardwareTestLogs.Add(operation);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            var concurrent = await _db.HardwareTestLogs.AsNoTracking().SingleAsync(
                x => x.TenantId == tenantId && x.RequestId == request.RequestId,
                cancellationToken);
            return concurrent.RequestPayloadHash == requestPayloadHash
                ? (null, MapTest(concurrent))
                : ("pos_hardware.request_id_conflict", null);
        }
        return (null, MapTest(operation));
    }

    public async Task<(string? ErrorCode, HardwareTestOperationDto? Operation)> CompleteTestAsync(
        Guid tenantId,
        Guid userId,
        Guid testId,
        CompleteHardwareTestRequest request,
        string? safeResultPayloadJson,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var operation = await _db.HardwareTestLogs.SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == testId,
            cancellationToken);
        if (operation is null) return ("pos_hardware.test_not_found", null);
        if (operation.TestedByTenantUserId != userId)
            return ("pos_hardware.permission_denied", null);
        if (operation.HardwareType == "BARCODESCANNER" &&
            (request.PhysicalConfirmation is null ||
             request.DetectedAt is null ||
             request.ScannerEvidence is null))
            return ("pos_hardware.invalid_test_result", null);
        if (operation.HardwareType == "CASHDRAWER" &&
            request.PhysicalConfirmation is null)
            return ("pos_hardware.invalid_test_result", null);

        var isFinal = operation.TestStatus is "PASSED" or "FAILED" or "UNKNOWN" or "CANCELLED" or "EXPIRED" or "BLOCKED";
        if (isFinal)
        {
            var same = operation.TestStatus.Equals(request.Status, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(operation.ResultCategory, request.ResultCategory, StringComparison.OrdinalIgnoreCase) &&
                       operation.PhysicalConfirmation == request.PhysicalConfirmation;
            return same ? (null, MapTest(operation)) : ("pos_hardware.result_conflict", null);
        }

        operation.Complete(
            request.Status, request.ResultCategory,
            SafeText(request.SafeMessage, 500), safeResultPayloadJson,
            request.PhysicalConfirmation, now);
        await _db.SaveChangesAsync(cancellationToken);
        return (null, MapTest(operation));
    }

    public async Task<IReadOnlyList<HardwareTestOperationDto>> GetTestHistoryAsync(
        Guid tenantId, Guid posDeviceId, int take, CancellationToken cancellationToken)
    {
        var rows = await _db.HardwareTestLogs.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.InitiatedFromPosDeviceId == posDeviceId)
            .OrderByDescending(x => x.TestedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
        return rows.Select(MapTest).ToList();
    }

    private async Task<TillSession?> ActiveSessionAsync(
        Guid tenantId,
        Guid posDeviceId,
        Guid? tillId,
        CancellationToken cancellationToken) =>
        await _db.TillSessions.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        (x.OpenedFromPosDeviceId == posDeviceId ||
                         (tillId != null && x.TillId == tillId)) &&
                        x.Status == "OPEN")
            .OrderByDescending(x => x.OpenedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private static PosHardwareConfigurationDto MapConfiguration(
        HardwareDevice device,
        Guid posDeviceId,
        Guid? tillId,
        Guid? tillSessionId,
        bool activeShift) =>
        new(device.Id, device.TenantId, device.OutletId, posDeviceId, tillId,
            ToCamel(device.HardwareDeviceType), ToCamel(device.ConnectionType),
            device.HardwareDeviceName, device.Status == "ACTIVE",
            device.ConfigurationVersion, activeShift, tillSessionId,
            ParseSettings(device.ConfigJson), device.UpdatedAt ?? device.CreatedAt);

    private static HardwareTestOperationDto MapTest(HardwareTestLog x)
    {
        var evidence = ParseTestEvidence(x.ResultPayloadJson);
        return new(x.Id, x.RequestId, x.InitiatedFromPosDeviceId!.Value,
            x.TillId, x.TillSessionId, x.HardwareDeviceId,
            ToCamel(x.HardwareType), ToCamel(x.TestType),
            x.ConfigurationVersion, ToTitle(x.TestStatus),
            x.ResultCategory?.ToLowerInvariant(), x.ResultMessage,
            x.PhysicalConfirmation, x.TestedAt, x.CompletedAt,
            evidence.DetectedAt, evidence.AutomaticResult,
            evidence.ScannerEvidence);
    }

    private static (
        DateTimeOffset? DetectedAt,
        string? AutomaticResult,
        ScannerTestEvidenceDto? ScannerEvidence) ParseTestEvidence(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (null, null, null);
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            DateTimeOffset? detectedAt = null;
            if (root.TryGetProperty("detectedAt", out var detected) &&
                detected.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(detected.GetString(), out var parsed))
                detectedAt = parsed;
            var automaticResult =
                root.TryGetProperty("automaticResult", out var automatic) &&
                automatic.ValueKind == JsonValueKind.String
                    ? automatic.GetString()
                    : null;
            ScannerTestEvidenceDto? scannerEvidence = null;
            if (root.TryGetProperty("scannerEvidence", out var evidence) &&
                evidence.ValueKind == JsonValueKind.Object)
                scannerEvidence = JsonSerializer.Deserialize<ScannerTestEvidenceDto>(
                    evidence.GetRawText(),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return (detectedAt, automaticResult, scannerEvidence);
        }
        catch (JsonException)
        {
            return (null, null, null);
        }
    }

    private static object ParseSettings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new { };
        try { return JsonDocument.Parse(json).RootElement.Clone(); }
        catch (JsonException) { return new { }; }
    }

    private static string SafeAuditJson(
        SavePosHardwareConfigurationRequest request, string settingsJson) =>
        JsonSerializer.Serialize(new
        {
            request.HardwareType,
            request.TransportType,
            request.DisplayName,
            request.Enabled,
            settings = ParseSettings(settingsJson)
        });

    private static string SafeAuditJson(HardwareDevice device) =>
        JsonSerializer.Serialize(new
        {
            hardwareType = ToCamel(device.HardwareDeviceType),
            transportType = ToCamel(device.ConnectionType),
            displayName = device.HardwareDeviceName,
            enabled = device.Status == "ACTIVE",
            settings = ParseSettings(device.ConfigJson)
        });

    private static string ToCamel(string value)
    {
        var compact = value.Replace("_", string.Empty).ToLowerInvariant();
        return compact switch
        {
            "receiptprinter" => "receiptPrinter",
            "barcodescanner" => "barcodeScanner",
            "cashdrawer" => "cashDrawer",
            "cardterminal" => "cardTerminal",
            "localprintagent" => "localPrintAgent",
            _ => compact
        };
    }

    private static string ToTitle(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();

    private static string? SafeText(string? value, int maxLength)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static bool ScannerModeSupportsTest(string? json, string testType)
    {
        try
        {
            using var document = JsonDocument.Parse(json ?? "{}");
            var mode = document.RootElement.TryGetProperty("mode", out var value)
                ? value.GetString()
                : null;
            var cameraTest = testType.Equals(
                                 "cameraInitialization",
                                 StringComparison.OrdinalIgnoreCase) ||
                             testType.Equals(
                                 "cameraScan",
                                 StringComparison.OrdinalIgnoreCase);
            return cameraTest
                ? mode == "camera"
                : mode is "hid" or "usbHid";
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
