using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
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

        var configError = await ValidateDeviceConfigurationAsync(context.TenantId, request, cancellationToken);
        if (configError is not null)
        {
            return ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Failure(configError);
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

        if (till.Status != TillConstants.ActiveStatus)
            return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Failure(
                new ApplicationError("hardware.validation_failed", "Only active tills can receive hardware assignments."));

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

        if (!posDevice.IsTrusted || posDevice.Status != PosDeviceConstants.ActiveStatus)
            return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Failure(
                new ApplicationError("hardware.pos_device_untrusted", "Only active, trusted POS devices can receive hardware assignments."));

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

        // Keep dependent drawers attached to their physical printer's target.
        for (var page = 1; ; page++)
        {
            var dependents = await _repository.ListAsync(context.TenantId, assignment.OutletId,
                "CASH_DRAWER", null, null, null, null, page, 100, cancellationToken);
            if (dependents.Items.Any(row => row.ActiveAssignment is not null &&
                TryGetParentPrinterId(row.Device.ConfigJson, out var parentId) &&
                parentId == assignment.HardwareDeviceId))
                return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Failure(
                    new ApplicationError("hardware.assignment_conflict", "Release attached cash drawers before releasing their printer."));
            if (page * 100 >= dependents.TotalCount || dependents.Items.Count == 0) break;
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
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return ApplicationResult<PosHardwareHeartbeatResponse>.Failure(
                new ApplicationError("hardware.invalid_tenant_context", "Invalid tenant context."));
        }

        if (!context.HasPermission(PosPermissions.Hardware.Settings) &&
            !context.HasPermission(TenantAdminTillPermissions.HardwareManage))
            return ApplicationResult<PosHardwareHeartbeatResponse>.Failure(
                new ApplicationError("hardware.permission_denied", "Permission denied for hardware telemetry."));
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

        if (request.Hardware is null || request.Hardware.Count is < 1 or > 64 ||
            request.Hardware.Any(item => item is null) ||
            request.Hardware.Select(item => item.HardwareDeviceId).Distinct().Count() != request.Hardware.Count)
        {
            return ApplicationResult<PosHardwareHeartbeatResponse>.Failure(
                new ApplicationError("hardware.validation_failed", "Supply between 1 and 64 distinct hardware items."));
        }

        var now = _dateTimeProvider.UtcNow;
        var observedAt = request.ObservedAt ?? now;
        if (request.ObservedAt is null || observedAt < now.AddMinutes(-5) || observedAt > now.AddSeconds(30))
            return ApplicationResult<PosHardwareHeartbeatResponse>.Failure(
                new ApplicationError("hardware.validation_failed", "Heartbeat observation time is outside the accepted window."));
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

            if (device.Status != "ACTIVE")
                return ApplicationResult<PosHardwareHeartbeatResponse>.Failure(
                    new ApplicationError("hardware.validation_failed", "Only active hardware can report a heartbeat."));
            if (item.ConfigurationVersion != device.ConfigurationVersion)
                return ApplicationResult<PosHardwareHeartbeatResponse>.Failure(
                    new ApplicationError("hardware.version_conflict", "Refresh the device configuration before reporting telemetry."));
            if (device.LastSeenAt >= observedAt) continue;
            device.RecordHeartbeat(observedAt);
            updated++;

            var status = string.Equals(item.ConnectionStatus, "DISCONNECTED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.HealthStatus, "FAILED", StringComparison.OrdinalIgnoreCase) ? "FAILED" :
                string.Equals(item.HealthStatus, "HEALTHY", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.ConnectionStatus, "CONNECTED", StringComparison.OrdinalIgnoreCase) ? "SUCCESS" : "WARNING";
            var previous = await _repository.GetLatestTelemetryAsync(context.TenantId, device.Id, cancellationToken);
            if (previous?.TestStatus != status || previous.ConfigurationVersion != device.ConfigurationVersion ||
                previous.InitiatedFromPosDeviceId != posDeviceId)
            {
                var payload = JsonSerializer.Serialize(new { status });

                var requestId = Guid.NewGuid();
                var log = HardwareTestLog.Create(
                    Guid.NewGuid(),
                    context.TenantId,
                    device.OutletId,
                    device.Id,
                    posDeviceId,
                    tillId: null,
                    tillSessionId: null,
                    context.UserId == Guid.Empty ? null : context.UserId,
                    requestId,
                    ComputePayloadHash(payload),
                    device.ConfigurationVersion,
                    device.HardwareDeviceType,
                    "TELEMETRY",
                    status,
                    resultCategory: null,
                    status == "SUCCESS" ? "Transport observed connected." : "Transport unavailable or needs attention.",
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
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return ApplicationResult<PosHardwareTestResultResponse>.Failure(
                new ApplicationError("hardware.invalid_tenant_context", "Invalid tenant context."));
        }

        if (!context.HasPermission(PosPermissions.Hardware.Settings) &&
            !context.HasPermission(TenantAdminTillPermissions.HardwareManage))
            return ApplicationResult<PosHardwareTestResultResponse>.Failure(
                new ApplicationError("hardware.permission_denied", "Permission denied for hardware tests."));
        if (string.IsNullOrWhiteSpace(request.TestType) || request.TestType.Length > 50 ||
            !request.TestType.All(c => char.IsAsciiLetterOrDigit(c) || c == '_') ||
            request.TestType.Equals("TELEMETRY", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(request.TestStatus) ||
            request.PosDeviceId is null || request.PosDeviceId == Guid.Empty ||
            request.RequestId is null || request.RequestId == Guid.Empty || request.ConfigurationVersion is null ||
            request.TestedAt is null || request.ResultCode?.Length > 80 ||
            (request.ResultCode is not null && !request.ResultCode.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')))
        {
            return ApplicationResult<PosHardwareTestResultResponse>.Failure(
                new ApplicationError("hardware.validation_failed", "Supply a device, request ID, configuration version, observation time and bounded test codes."));
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

            if (!posDevice.IsTrusted || posDevice.Status != "ACTIVE")
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
        var testedAt = request.TestedAt!.Value;
        var requestId = request.RequestId!.Value;
        // Hash the supplied observation, excluding free-text messages which are never persisted.
        var hash = ComputePayloadHash(JsonSerializer.Serialize(new {
            request.HardwareDeviceId, request.PosDeviceId, request.ConfigurationVersion,
            request.TestType, request.TestStatus, request.ResultCode, testedAt
        }));
        var previous = await _repository.GetTestByRequestIdAsync(context.TenantId, requestId, cancellationToken);
        if (previous is not null)
        {
            if (previous.RequestPayloadHash != hash || previous.TestedByTenantUserId != context.UserId)
                return ApplicationResult<PosHardwareTestResultResponse>.Failure(new("hardware.idempotency_conflict", "The request ID was used for another observation."));
            return ApplicationResult<PosHardwareTestResultResponse>.Success(new(previous.Id, device.Id,
                previous.TestType, previous.TestStatus, previous.TestedAt));
        }
        if (testedAt < now.AddMinutes(-5) || testedAt > now.AddSeconds(30) || device.Status != "ACTIVE")
            return ApplicationResult<PosHardwareTestResultResponse>.Failure(new("hardware.validation_failed", "Report a recent observation for active hardware."));
        if (device.ConfigurationVersion != request.ConfigurationVersion)
            return ApplicationResult<PosHardwareTestResultResponse>.Failure(new("hardware.version_conflict", "Refresh the hardware configuration before testing."));
        var payload = request.ResultCode is null
            ? null
            : JsonSerializer.Serialize(new { resultCode = request.ResultCode });

        var log = HardwareTestLog.Create(
            Guid.NewGuid(),
            context.TenantId,
            device.OutletId,
            device.Id,
            posDeviceId,
            tillId: null,
            tillSessionId: null,
            context.UserId == Guid.Empty ? null : context.UserId,
            requestId,
            hash,
            device.ConfigurationVersion,
            device.HardwareDeviceType,
            request.TestType,
            request.TestStatus,
            resultCategory: null,
            "POS diagnostic observation reported. Physical acceptance uses the hardware test workflow.",
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

        if (!string.Equals(device.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Failure(
                new ApplicationError("hardware.validation_failed", "Only active hardware can be assigned."));
        }

        if (TryGetParentPrinterId(device.ConfigJson, out var parentId))
        {
            var parent = await _repository.GetEditableDeviceAsync(context.TenantId, parentId, cancellationToken);
            var parentAssignment = await _repository.GetActiveAssignmentForDeviceAsync(context.TenantId, parentId, cancellationToken);
            if (parent is null || parent.OutletId != expectedOutletId ||
                parent.HardwareDeviceType != "RECEIPT_PRINTER" || parent.Status != "ACTIVE" || !HasDrawerConfiguration(parent) ||
                parentAssignment is null || parentAssignment.TillId != tillId || parentAssignment.PosDeviceId != posDeviceId)
            {
                return ApplicationResult<TenantAdminHardwareAssignmentResponse>.Failure(
                    new ApplicationError("hardware.validation_failed", "Assign the active parent printer to the same target first."));
            }
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

    private async Task<ApplicationError?> ValidateDeviceConfigurationAsync(
        Guid tenantId, TenantAdminHardwareDeviceCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.HardwareDeviceType.Equals("SCALE", StringComparison.OrdinalIgnoreCase) ||
            request.HardwareDeviceType.Equals("CUSTOMER_DISPLAY", StringComparison.OrdinalIgnoreCase))
            return new ApplicationError("hardware.unsupported", "This device family has no supported runtime adapter.");
        if (request.HardwareDeviceType.Equals("CARD_READER", StringComparison.OrdinalIgnoreCase) &&
            !request.ConnectionType.Equals("PROVIDER", StringComparison.OrdinalIgnoreCase))
            return new ApplicationError("hardware.validation_failed", "Payment terminals require a provider integration.");
        var matchingProfiles = HardwareCompatibilityCatalog.Search(deviceType: request.HardwareDeviceType.Trim(),
            connectionType: request.ConnectionType.Trim()).Where(p => p.SupportLevel != "UNSUPPORTED").ToArray();
        if (matchingProfiles.Length == 0)
            return new ApplicationError("hardware.unsupported", "No runtime adapter is available for this device and connection.");
        if (request.ConfigJson?.Length > 8192)
            return new ApplicationError("hardware.validation_failed", "Device configuration exceeds the size limit.");
        if (request.HardwareDeviceType.Equals("CASH_DRAWER", StringComparison.OrdinalIgnoreCase) &&
            !TryGetParentPrinterId(request.ConfigJson, out _))
            return new ApplicationError("hardware.validation_failed", "A configured parent printer is required for the cash drawer.");
        if (string.IsNullOrWhiteSpace(request.ConfigJson)) return null;
        try
        {
            using var json = JsonDocument.Parse(request.ConfigJson);
            if (json.RootElement.ValueKind != JsonValueKind.Object)
                return new ApplicationError("hardware.validation_failed", "Device configuration must be a JSON object.");
            // Registry configuration is metadata, never a provider secret store.
            var allowed = new HashSet<string>(StringComparer.Ordinal) {
                "protocol", "host", "ip", "port", "paperWidth", "autoCut", "cashDrawer", "qrPrint",
                "parentPrinterId", "connectionStyle", "provider", "terminalId", "usbVendorId",
                "usbProductId", "usbDeviceName", "bluetoothAddress", "adapterKey",
                "compatibilityProfileId", "capabilitySource"
            };
            if (json.RootElement.EnumerateObject().Any(p => !allowed.Contains(p.Name) ||
                p.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array))
                return new ApplicationError("hardware.validation_failed", "Configuration contains unsupported fields. Store credentials in secure application configuration.");
            if (json.RootElement.EnumerateObject().GroupBy(p => p.Name).Any(g => g.Count() > 1))
                return new ApplicationError("hardware.validation_failed", "Duplicate configuration fields are not allowed.");
            foreach (var field in new[] { "cashDrawer", "autoCut", "qrPrint" })
                if (json.RootElement.TryGetProperty(field, out var flag) &&
                    flag.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return new ApplicationError("hardware.validation_failed", "Printer capability flags must be boolean values.");
            if (json.RootElement.TryGetProperty("paperWidth", out var paper) &&
                (paper.ValueKind != JsonValueKind.Number || !paper.TryGetInt32(out var width) || width is not (58 or 80)))
                return new ApplicationError("hardware.validation_failed", "Paper width must be 58 or 80 mm.");
            foreach (var field in new[] { "protocol", "adapterKey", "capabilitySource" })
            {
                if (!json.RootElement.TryGetProperty(field, out var value)) continue;
                if (value.ValueKind != JsonValueKind.String || !matchingProfiles.Any(p =>
                    string.Equals(value.GetString(), field switch {
                        "protocol" => p.Protocol, "adapterKey" => p.AdapterKey, _ => p.CapabilitySource
                    }, StringComparison.OrdinalIgnoreCase)))
                    return new ApplicationError("hardware.validation_failed", "Configuration metadata does not match the available adapter.");
            }
            if (json.RootElement.TryGetProperty("compatibilityProfileId", out var profileId))
            {
                var profile = HardwareCompatibilityCatalog.Profiles.FirstOrDefault(p =>
                    profileId.ValueKind == JsonValueKind.String && p.Id == profileId.GetString());
                if (profile is null || profile.SupportLevel == "UNSUPPORTED" ||
                    !profile.DeviceType.Equals(request.HardwareDeviceType, StringComparison.OrdinalIgnoreCase) ||
                    !profile.ConnectionType.Equals(request.ConnectionType, StringComparison.OrdinalIgnoreCase))
                    return new ApplicationError("hardware.unsupported", "The compatibility profile does not support this device and connection.");
            }
            if (json.RootElement.TryGetProperty("parentPrinterId", out var parent))
            {
                if (!request.HardwareDeviceType.Equals("CASH_DRAWER", StringComparison.OrdinalIgnoreCase) ||
                    parent.ValueKind != JsonValueKind.String || !Guid.TryParse(parent.GetString(), out var id))
                    return new ApplicationError("hardware.validation_failed", "Invalid parent printer.");
                var device = await _repository.GetEditableDeviceAsync(tenantId, id, cancellationToken);
                if (device is null || device.OutletId != request.OutletId ||
                    device.HardwareDeviceType != "RECEIPT_PRINTER" || device.Status != "ACTIVE" || !HasDrawerConfiguration(device))
                    return new ApplicationError("hardware.validation_failed", "Select an active ESC/POS parent printer in the same outlet with cash drawer support enabled.");
            }
        }
        catch (JsonException)
        {
            return new ApplicationError("hardware.validation_failed", "Device configuration must be valid JSON.");
        }
        return null;
    }

    public async Task<ApplicationResult<TenantAdminHardwareDeviceDetailResponse>> UpdateAsync(TenantRequestContext context,
        Guid id, TenantAdminHardwareUpdateRequest request, CancellationToken cancellationToken)
    {
        var denied = RequireManage(context);
        if (denied is not null) return ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Failure(denied);
        if (string.IsNullOrWhiteSpace(request.HardwareDeviceName) || request.HardwareDeviceName.Trim().Length > 150 ||
            !AllowedLifecycleStatuses.Contains(request.Status ?? "") || request.ExpectedVersion < 1)
            return ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Failure(new("hardware.validation_failed", "Provide a name, valid lifecycle status and current version."));
        var device = await _repository.GetEditableDeviceAsync(context.TenantId, id, cancellationToken);
        if (device is null) return ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Failure(new("hardware.not_found", "Hardware device was not found."));
        if (device.ConfigurationVersion != request.ExpectedVersion)
            return ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Failure(new("hardware.version_conflict", "Device changed. Refresh before editing."));
        if (request.Status != "ACTIVE" && await _repository.GetActiveAssignmentForDeviceAsync(context.TenantId, id, cancellationToken) is not null)
            return ApplicationResult<TenantAdminHardwareDeviceDetailResponse>.Failure(new("hardware.assignment_conflict", "Release the device and dependent drawers before deactivating it."));
        device.UpdateConfiguration(request.HardwareDeviceName, device.ConnectionType, device.ConfigJson,
            request.Status!, request.ExpectedVersion, context.UserId, _dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(context, id, cancellationToken);
    }

    private static bool HasDrawerConfiguration(HardwareDevice printer)
    {
        if (string.IsNullOrWhiteSpace(printer.ConfigJson)) return false;
        try
        {
            using var json = JsonDocument.Parse(printer.ConfigJson);
            return json.RootElement.ValueKind == JsonValueKind.Object &&
                json.RootElement.TryGetProperty("cashDrawer", out var enabled) && enabled.ValueKind == JsonValueKind.True &&
                json.RootElement.TryGetProperty("protocol", out var protocol) && protocol.ValueKind == JsonValueKind.String &&
                protocol.GetString() == "ESC/POS" && HardwareCompatibilityCatalog.Search(deviceType: "RECEIPT_PRINTER",
                    connectionType: printer.ConnectionType).Any(p => p.SupportLevel != "UNSUPPORTED");
        }
        catch (JsonException) { return false; }
    }

    private static bool TryGetParentPrinterId(string? config, out Guid parentId)
    {
        parentId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(config)) return false;
        try
        {
            using var json = JsonDocument.Parse(config);
            return json.RootElement.ValueKind == JsonValueKind.Object &&
                json.RootElement.TryGetProperty("parentPrinterId", out var parent) &&
                parent.ValueKind == JsonValueKind.String && Guid.TryParse(parent.GetString(), out parentId);
        }
        catch (JsonException) { return false; }
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

        if (string.IsNullOrWhiteSpace(request.HardwareDeviceType) || !AllowedHardwareTypes.Contains(request.HardwareDeviceType.Trim()))
        {
            return new ApplicationError("hardware.validation_failed", "Invalid hardware type.");
        }

        if (string.IsNullOrWhiteSpace(request.ConnectionType) || !AllowedConnectionTypes.Contains(request.ConnectionType.Trim()))
        {
            return new ApplicationError("hardware.validation_failed", "Invalid connection type.");
        }

        if (string.IsNullOrWhiteSpace(request.Status) || !AllowedLifecycleStatuses.Contains(request.Status.Trim()))
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
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or OverflowException)
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
            assignment?.PosDeviceId,
            row.Device.ConfigurationVersion);
    }

    private static string ComputePayloadHash(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
