using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using TenantAdminTillPermissions = E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants.TenantAdminTillPermissions;

namespace E_POS.Application.Modules.Tenant.HardwareCash.Services;

public sealed class TenantAdminHardwareService : ITenantAdminHardwareService
{
    private static readonly HashSet<string> AllowedHardwareTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "RECEIPT_PRINTER",
        "BARCODE_SCANNER",
        "CASH_DRAWER",
        "CARD_READER",
        "CUSTOMER_DISPLAY",
        "SCALE",
        "BUILT_IN_CAMERA_SCANNER",
    };

    private static readonly HashSet<string> AllowedConnectionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "NETWORK",
        "USB",
        "BLUETOOTH",
        "BUILT_IN",
        "PROVIDER",
        "SERIAL",
    };

    private static readonly HashSet<string> AllowedLifecycleStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "ACTIVE",
        "INACTIVE",
        "MAINTENANCE",
    };

    private static readonly HashSet<string> AllowedTestStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "PENDING",
        "PASSED",
        "SUCCESS",
        "FAILED",
        "WARNING",
        "TIMEOUT",
        "NOT_SUPPORTED",
        "ERROR",
    };

    private readonly ITenantAdminHardwareRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITenantAdminHardwareAuditLogger _auditLogger;

    public TenantAdminHardwareService(
        ITenantAdminHardwareRepository repository,
        IDateTimeProvider dateTimeProvider,
        ITenantAdminHardwareAuditLogger auditLogger)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
        _auditLogger = auditLogger;
    }

    public async Task<ApplicationResult<TenantAdminHardwareDeviceListResponse>> ListAsync(
        TenantRequestContext context,
        Guid? outletId,
        string? hardwareType,
        string? lifecycleStatus,
        string? assignmentStatus,
        bool? availableOnly,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var accessError = RequireView(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminHardwareDeviceListResponse>.Failure(accessError);
        }

        if (outletId.HasValue &&
            !await _repository.OutletBelongsToTenantAsync(context.TenantId, outletId.Value, cancellationToken))
        {
            return ApplicationResult<TenantAdminHardwareDeviceListResponse>.Failure(
                new ApplicationError("hardware.outlet_not_found", "Outlet was not found for this tenant."));
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _repository.ListAsync(
            context.TenantId,
            outletId,
            hardwareType,
            lifecycleStatus,
            assignmentStatus,
            availableOnly,
            search,
            safePage,
            safePageSize,
            cancellationToken);

        var mapped = items.Select(MapListItem).ToList();
        return ApplicationResult<TenantAdminHardwareDeviceListResponse>.Success(
            new TenantAdminHardwareDeviceListResponse(mapped, safePage, safePageSize, total));
    }

    public async Task<ApplicationResult<TenantAdminHardwareDeviceDetailResponse>> GetByIdAsync(
        TenantRequestContext context,
        Guid hardwareDeviceId,
        CancellationToken cancellationToken)
    {
        var accessError = RequireView(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Failure(accessError);
        }

        var row = await _repository.GetDetailAsync(context.TenantId, hardwareDeviceId, cancellationToken);
        if (row is null)
        {
            return ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Failure(
                new ApplicationError("hardware.not_found", "Hardware device was not found."));
        }

        return ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Success(MapDetail(row));
    }

    public async Task<ApplicationResult<TenantAdminHardwareDeviceDetailResponse>> CreateAsync(
        TenantRequestContext context,
        TenantAdminHardwareDeviceCreateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = RequireManage(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Failure(accessError);
        }

        var validation = ValidateCreate(request);
        if (validation is not null)
        {
            return ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Failure(validation);
        }

        if (!await _repository.OutletBelongsToTenantAsync(context.TenantId, request.OutletId, cancellationToken))
        {
            return ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Failure(
                new ApplicationError("hardware.outlet_not_found", "Outlet was not found for this tenant."));
        }

        var normalizedCode = request.HardwareDeviceCode.Trim().ToUpperInvariant();
        if (await _repository.DeviceCodeExistsAsync(context.TenantId, normalizedCode, null, cancellationToken))
        {
            return ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Failure(
                new ApplicationError("hardware.duplicate_code", "Hardware device code already exists for this tenant."));
        }

        if (request.ConnectionType.Equals("NETWORK", StringComparison.OrdinalIgnoreCase))
        {
            var networkError = ValidateNetworkConfig(request.ConfigJson);
            if (networkError is not null)
            {
                return ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Failure(networkError);
            }
        }

        var now = _dateTimeProvider.UtcNow;
        var actor = context.UserId == Guid.Empty ? (Guid?)null : context.UserId;
        var device = HardwareDevice.Create(
            Guid.NewGuid(),
            context.TenantId,
            request.OutletId,
            null,
            normalizedCode,
            request.HardwareDeviceName,
            request.HardwareDeviceType,
            request.ConnectionType,
            request.Manufacturer,
            request.Model,
            request.SerialNumber,
            request.AssetTag,
            request.FirmwareVersion,
            request.ConfigJson,
            request.Status,
            actor,
            now);

        await _repository.AddDeviceAsync(device, cancellationToken);
        _auditLogger.LogHardwareCreated(context.TenantId, actor, device.Id, device.HardwareDeviceCode, device.HardwareDeviceType);

        var detail = await _repository.GetDetailAsync(context.TenantId, device.Id, cancellationToken);
        return detail is null
            ? ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Failure(
                new ApplicationError("hardware.not_found", "Hardware device was not found."))
            : ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Success(MapDetail(detail));
    }

    public async Task<ApplicationResult<TenantAdminHardwareAssignmentResponse>> AssignToTillAsync(
        TenantRequestContext context,
        Guid tillId,
        TenantAdminHardwareAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = RequireManage(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Failure(accessError);
        }

        var till = await _repository.GetTillAsync(context.TenantId, tillId, cancellationToken);
        if (till is null)
        {
            return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Failure(
                new ApplicationError("hardware.till_not_found", "Till was not found."));
        }

        return await AssignInternalAsync(
            context,
            request.HardwareDeviceId,
            till.OutletId,
            tillId,
            null,
            request.IsPrimary,
            cancellationToken);
    }

    public async Task<ApplicationResult<TenantAdminHardwareAssignmentResponse>> AssignToPosDeviceAsync(
        TenantRequestContext context,
        Guid posDeviceId,
        TenantAdminHardwareAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = RequireManage(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Failure(accessError);
        }

        var posDevice = await _repository.GetPosDeviceAsync(context.TenantId, posDeviceId, cancellationToken);
        if (posDevice is null)
        {
            return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Failure(
                new ApplicationError("hardware.pos_device_not_found", "POS device was not found."));
        }

        return await AssignInternalAsync(
            context,
            request.HardwareDeviceId,
            posDevice.OutletId,
            null,
            posDeviceId,
            request.IsPrimary,
            cancellationToken);
    }

    public async Task<ApplicationResult<TenantAdminHardwareAssignmentResponse>> ReleaseAssignmentAsync(
        TenantRequestContext context,
        Guid assignmentId,
        TenantAdminHardwareAssignmentReleaseRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = RequireManage(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Failure(accessError);
        }

        var assignment = await _repository.GetAssignmentAsync(context.TenantId, assignmentId, cancellationToken);
        if (assignment is null)
        {
            return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Failure(
                new ApplicationError("hardware.assignment_not_found", "Hardware assignment was not found."));
        }

        if (assignment.ReleasedAt is not null)
        {
            return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Success(
                new TenantAdminHardwareAssignmentResponse(
                    assignment.Id,
                    assignment.HardwareDeviceId,
                    assignment.OutletId,
                    assignment.TillId,
                    assignment.PosDeviceId,
                    assignment.IsPrimary,
                    assignment.AssignedAt));
        }

        var actor = context.UserId == Guid.Empty ? (Guid?)null : context.UserId;
        assignment.Release(request.Reason, actor, _dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditLogger.LogHardwareReleased(
            context.TenantId,
            actor,
            assignment.Id,
            assignment.HardwareDeviceId,
            request.Reason);

        return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Success(
            new TenantAdminHardwareAssignmentResponse(
                assignment.Id,
                assignment.HardwareDeviceId,
                assignment.OutletId,
                assignment.TillId,
                assignment.PosDeviceId,
                assignment.IsPrimary,
                assignment.AssignedAt));
    }

    public async Task<ApplicationResult<PosHardwareHeartbeatResponse>> RecordHardwareHeartbeatAsync(
        TenantRequestContext context,
        Guid posDeviceId,
        PosHardwareHeartbeatRequest request,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty)
        {
            return ApplicationResult<PosHardwareHeartbeatResponse>.Failure(
                new ApplicationError("hardware.invalid_tenant_context", "Invalid tenant context."));
        }

        var posDevice = await _repository.GetPosDeviceAsync(context.TenantId, posDeviceId, cancellationToken);
        if (posDevice is null)
        {
            return ApplicationResult<PosHardwareHeartbeatResponse>.Failure(
                new ApplicationError("hardware.pos_device_not_found", "POS device was not found."));
        }

        if (!posDevice.IsTrusted ||
            !string.Equals(posDevice.Status, PosDeviceConstants.ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<PosHardwareHeartbeatResponse>.Failure(
                new ApplicationError("hardware.pos_device_untrusted", "POS device is not trusted or active."));
        }

        if (request.Hardware is null || request.Hardware.Count == 0)
        {
            return ApplicationResult<PosHardwareHeartbeatResponse>.Failure(
                new ApplicationError("hardware.validation_failed", "At least one hardware item is required."));
        }

        var now = _dateTimeProvider.UtcNow;
        var observedAt = request.ObservedAt ?? now;
        var updated = 0;

        foreach (var item in request.Hardware)
        {
            var device = await _repository.GetEditableDeviceAsync(
                context.TenantId,
                item.HardwareDeviceId,
                cancellationToken);
            if (device is null)
            {
                return ApplicationResult<PosHardwareHeartbeatResponse>.Failure(
                    new ApplicationError("hardware.not_found", "Hardware device was not found."));
            }

            var linked = await _repository.IsHardwareLinkedToPosDeviceAsync(
                context.TenantId,
                posDeviceId,
                item.HardwareDeviceId,
                cancellationToken);
            if (!linked)
            {
                return ApplicationResult<PosHardwareHeartbeatResponse>.Failure(
                    new ApplicationError(
                        "hardware.unrelated_device",
                        "Hardware device is not assigned to this POS device or its till."));
            }

            device.RecordHeartbeat(observedAt);
            updated++;

            if (!string.IsNullOrWhiteSpace(item.WarningCode) ||
                string.Equals(item.HealthStatus, "WARNING", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.HealthStatus, "FAILED", StringComparison.OrdinalIgnoreCase))
            {
                var status = string.Equals(item.HealthStatus, "FAILED", StringComparison.OrdinalIgnoreCase)
                    ? "FAILED"
                    : "WARNING";

                var payload = JsonSerializer.Serialize(new
                {
                    item.WarningCode,
                    item.WarningMessage,
                    item.ConnectionStatus,
                    item.HealthStatus,
                });

                var log = HardwareTestLog.Create(
                    Guid.NewGuid(),
                    context.TenantId,
                    device.OutletId,
                    device.Id,
                    posDeviceId,
                    context.UserId == Guid.Empty ? null : context.UserId,
                    "TELEMETRY",
                    status,
                    item.WarningMessage,
                    payload,
                    observedAt,
                    now);
                await _repository.AddTestLogAsync(log, cancellationToken);
                _auditLogger.LogHardwareHeartbeat(context.TenantId, posDeviceId, device.Id, item.WarningCode);
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult<PosHardwareHeartbeatResponse>.Success(
            new PosHardwareHeartbeatResponse(posDeviceId, now, updated));
    }

    public async Task<ApplicationResult<PosHardwareTestResultResponse>> ReportHardwareTestAsync(
        TenantRequestContext context,
        PosHardwareTestResultRequest request,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty)
        {
            return ApplicationResult<PosHardwareTestResultResponse>.Failure(
                new ApplicationError("hardware.invalid_tenant_context", "Invalid tenant context."));
        }

        if (string.IsNullOrWhiteSpace(request.TestType) || string.IsNullOrWhiteSpace(request.TestStatus))
        {
            return ApplicationResult<PosHardwareTestResultResponse>.Failure(
                new ApplicationError("hardware.validation_failed", "Test type and status are required."));
        }

        if (!AllowedTestStatuses.Contains(request.TestStatus.Trim()))
        {
            return ApplicationResult<PosHardwareTestResultResponse>.Failure(
                new ApplicationError("hardware.validation_failed", "Invalid test status."));
        }

        var device = await _repository.GetEditableDeviceAsync(
            context.TenantId,
            request.HardwareDeviceId,
            cancellationToken);
        if (device is null)
        {
            return ApplicationResult<PosHardwareTestResultResponse>.Failure(
                new ApplicationError("hardware.not_found", "Hardware device was not found."));
        }

        Guid? posDeviceId = request.PosDeviceId;
        if (posDeviceId.HasValue)
        {
            var posDevice = await _repository.GetPosDeviceAsync(context.TenantId, posDeviceId.Value, cancellationToken);
            if (posDevice is null)
            {
                return ApplicationResult<PosHardwareTestResultResponse>.Failure(
                    new ApplicationError("hardware.pos_device_not_found", "POS device was not found."));
            }

            if (!posDevice.IsTrusted)
            {
                return ApplicationResult<PosHardwareTestResultResponse>.Failure(
                    new ApplicationError("hardware.pos_device_untrusted", "POS device is not trusted."));
            }

            var linked = await _repository.IsHardwareLinkedToPosDeviceAsync(
                context.TenantId,
                posDeviceId.Value,
                request.HardwareDeviceId,
                cancellationToken);
            if (!linked)
            {
                return ApplicationResult<PosHardwareTestResultResponse>.Failure(
                    new ApplicationError(
                        "hardware.unrelated_device",
                        "Hardware device is not assigned to this POS device or its till."));
            }
        }

        var now = _dateTimeProvider.UtcNow;
        var testedAt = request.TestedAt ?? now;
        var payload = request.ResultCode is null
            ? null
            : JsonSerializer.Serialize(new { resultCode = request.ResultCode });

        var log = HardwareTestLog.Create(
            Guid.NewGuid(),
            context.TenantId,
            device.OutletId,
            device.Id,
            posDeviceId,
            context.UserId == Guid.Empty ? null : context.UserId,
            request.TestType,
            request.TestStatus,
            request.ResultMessage,
            payload,
            testedAt,
            now);

        await _repository.AddTestLogAsync(log, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        if (log.TestStatus is "FAILED" or "ERROR" or "TIMEOUT")
        {
            _auditLogger.LogHardwareTestFailed(
                context.TenantId,
                device.Id,
                log.TestType,
                log.ResultMessage);
        }

        return ApplicationResult<PosHardwareTestResultResponse>.Success(
            new PosHardwareTestResultResponse(log.Id, device.Id, log.TestType, log.TestStatus, log.TestedAt));
    }

    private async Task<ApplicationResult<TenantAdminHardwareAssignmentResponse>> AssignInternalAsync(
        TenantRequestContext context,
        Guid hardwareDeviceId,
        Guid expectedOutletId,
        Guid? tillId,
        Guid? posDeviceId,
        bool isPrimary,
        CancellationToken cancellationToken)
    {
        var device = await _repository.GetEditableDeviceAsync(context.TenantId, hardwareDeviceId, cancellationToken);
        if (device is null)
        {
            return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Failure(
                new ApplicationError("hardware.not_found", "Hardware device was not found."));
        }

        if (device.OutletId != expectedOutletId)
        {
            return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Failure(
                new ApplicationError("hardware.outlet_mismatch", "Hardware device outlet does not match the assignment target."));
        }

        if (string.Equals(device.Status, "DELETED", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Failure(
                new ApplicationError("hardware.validation_failed", "Deleted hardware cannot be assigned."));
        }

        var existing = await _repository.GetActiveAssignmentForDeviceAsync(
            context.TenantId,
            hardwareDeviceId,
            cancellationToken);
        if (existing is not null)
        {
            return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Failure(
                new ApplicationError("hardware.assignment_conflict", "Hardware device already has an active assignment."));
        }

        var now = _dateTimeProvider.UtcNow;
        var actor = context.UserId == Guid.Empty ? (Guid?)null : context.UserId;
        var assignment = HardwareDeviceAssignment.Create(
            Guid.NewGuid(),
            context.TenantId,
            device.OutletId,
            hardwareDeviceId,
            tillId,
            posDeviceId,
            isPrimary,
            actor,
            now);

        await _repository.AddAssignmentAsync(assignment, cancellationToken);
        _auditLogger.LogHardwareAssigned(
            context.TenantId,
            actor,
            assignment.Id,
            hardwareDeviceId,
            tillId,
            posDeviceId);

        return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Success(
            new TenantAdminHardwareAssignmentResponse(
                assignment.Id,
                assignment.HardwareDeviceId,
                assignment.OutletId,
                assignment.TillId,
                assignment.PosDeviceId,
                assignment.IsPrimary,
                assignment.AssignedAt));
    }

    private static ApplicationError? ValidateCreate(TenantAdminHardwareDeviceCreateRequest request)
    {
        if (request.OutletId == Guid.Empty)
        {
            return new ApplicationError("hardware.validation_failed", "Outlet is required.");
        }

        if (string.IsNullOrWhiteSpace(request.HardwareDeviceCode) || request.HardwareDeviceCode.Trim().Length > 80)
        {
            return new ApplicationError("hardware.validation_failed", "Hardware device code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.HardwareDeviceName) || request.HardwareDeviceName.Trim().Length > 150)
        {
            return new ApplicationError("hardware.validation_failed", "Hardware device name is required.");
        }

        if (!AllowedHardwareTypes.Contains(request.HardwareDeviceType.Trim()))
        {
            return new ApplicationError("hardware.validation_failed", "Invalid hardware type.");
        }

        if (!AllowedConnectionTypes.Contains(request.ConnectionType.Trim()))
        {
            return new ApplicationError("hardware.validation_failed", "Invalid connection type.");
        }

        if (!AllowedLifecycleStatuses.Contains(request.Status.Trim()))
        {
            return new ApplicationError("hardware.validation_failed", "Invalid lifecycle status.");
        }

        return null;
    }

    private static ApplicationError? ValidateNetworkConfig(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(configJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("host", out var host) || root.TryGetProperty("ip", out host))
            {
                var hostValue = host.GetString();
                if (string.IsNullOrWhiteSpace(hostValue) || hostValue.Length > 255)
                {
                    return new ApplicationError("hardware.validation_failed", "Invalid network host.");
                }
            }

            if (root.TryGetProperty("port", out var portElement))
            {
                var port = portElement.ValueKind == JsonValueKind.Number
                    ? portElement.GetInt32()
                    : int.TryParse(portElement.GetString(), out var parsed) ? parsed : -1;
                if (port is < 1 or > 65535)
                {
                    return new ApplicationError("hardware.validation_failed", "Invalid network port.");
                }
            }
        }
        catch (JsonException)
        {
            return new ApplicationError("hardware.validation_failed", "ConfigJson must be valid JSON.");
        }

        return null;
    }

    private static ApplicationError? RequireView(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("hardware.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(TenantAdminTillPermissions.HardwareView) ||
               context.HasPermission(TenantAdminTillPermissions.HardwareManage)
            ? null
            : new ApplicationError("hardware.permission_denied", "Permission denied for hardware.");
    }

    private static ApplicationError? RequireManage(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("hardware.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(TenantAdminTillPermissions.HardwareManage)
            ? null
            : new ApplicationError("hardware.permission_denied", "Permission denied for hardware management.");
    }

    private static TenantAdminHardwareDeviceListItemResponse MapListItem(HardwareDeviceListRow row)
    {
        var assignment = row.ActiveAssignment;
        return new TenantAdminHardwareDeviceListItemResponse(
            row.Device.Id,
            row.Device.HardwareDeviceCode,
            row.Device.HardwareDeviceName,
            row.Device.HardwareDeviceType,
            row.Device.ConnectionType,
            row.Device.Status,
            row.Device.OutletId,
            row.OutletName,
            row.Device.Manufacturer,
            row.Device.Model,
            row.Device.SerialNumber,
            row.Device.LastSeenAt,
            assignment is not null,
            assignment?.TillId,
            assignment?.PosDeviceId);
    }

    private static TenantAdminHardwareDeviceDetailResponse MapDetail(HardwareDeviceDetailRow row)
    {
        var assignment = row.ActiveAssignment;
        return new TenantAdminHardwareDeviceDetailResponse(
            row.Device.Id,
            row.Device.HardwareDeviceCode,
            row.Device.HardwareDeviceName,
            row.Device.HardwareDeviceType,
            row.Device.ConnectionType,
            row.Device.Status,
            row.Device.OutletId,
            row.OutletName,
            row.Device.Manufacturer,
            row.Device.Model,
            row.Device.SerialNumber,
            row.Device.AssetTag,
            row.Device.FirmwareVersion,
            row.Device.ConfigJson,
            row.Device.LastSeenAt,
            row.Device.CreatedAt,
            row.Device.UpdatedAt ?? row.Device.CreatedAt,
            assignment is not null,
            assignment?.Id,
            assignment?.TillId,
            assignment?.PosDeviceId);
    }
}
