using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Options;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using Microsoft.Extensions.Options;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Services;

public sealed class TenantAdminTillService : ITenantAdminTillService
{
    private static readonly ApplicationError PermissionDenied = new(
        "till.permission_denied",
        "Permission denied for till management.");
    private static readonly ApplicationError NotFound = new("till.not_found", "Till was not found.");
    private static readonly ApplicationError OutletNotFound = new(
        "till.outlet_not_found",
        "Outlet was not found for this tenant.");

    private readonly ITenantAdminTillRepository _repository;
    private readonly ITenantAdminHardwareRepository _hardwareRepository;
    private readonly ITillDeviceAssignmentRepository _assignmentRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IOptionsSnapshot<TillMonitoringOptions> _options;
    private readonly ITenantResourceLimitGuard _resourceLimitGuard;

    public TenantAdminTillService(
        ITenantAdminTillRepository repository,
        ITenantAdminHardwareRepository hardwareRepository,
        ITillDeviceAssignmentRepository assignmentRepository,
        IDateTimeProvider dateTimeProvider,
        IOptionsSnapshot<TillMonitoringOptions> options,
        ITenantResourceLimitGuard resourceLimitGuard)
    {
        _repository = repository;
        _hardwareRepository = hardwareRepository;
        _assignmentRepository = assignmentRepository;
        _dateTimeProvider = dateTimeProvider;
        _options = options;
        _resourceLimitGuard = resourceLimitGuard;
    }

    public async Task<ApplicationResult<TenantAdminTillListResponse>> ListAsync(
        TenantRequestContext context,
        string? search,
        string? status,
        Guid? outletId,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(
            context,
            TenantAdminTillPermissions.View,
            TenantAdminTillPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminTillListResponse>.Failure(accessError);
        }

        if (outletId.HasValue &&
            !await _repository.OutletBelongsToTenantAsync(context.TenantId, outletId.Value, cancellationToken))
        {
            return ApplicationResult<TenantAdminTillListResponse>.Failure(OutletNotFound);
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var (items, totalCount) = await _repository.ListAsync(
            context.TenantId,
            search,
            status,
            outletId,
            safePage,
            safePageSize,
            sortBy ?? "name",
            sortDirection ?? "asc",
            cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var timeout = _options.Value.HeartbeatTimeoutSeconds;

        var mappedItems = items.Select(model => {
            var resolvedStatus = TillMonitoringStatusResolver.Resolve(
                model.Till.Status,
                model.AssignedDevice != null, // Wait, active assignment is inferred from AssignedDevice != null here
                model.AssignedDevice?.Status,
                model.AssignedDevice?.IsTrusted ?? false,
                model.AssignedDevice?.LastSeenAt,
                now,
                timeout
            );

            return new TenantAdminTillListItemResponse(
                model.Till.Id,
                model.Till.TillName,
                model.Till.TillCode,
                model.Outlet.Id,
                model.Outlet.OutletName,
                FormatStatus(model.Till.Status),
                model.AssignedDevice?.Status,
                model.Till.UpdatedAt ?? model.Till.CreatedAt, // Or use model.AssignedDevice.LastSeenAt? Original code used last session updated at
                resolvedStatus.NeedsAttention,
                resolvedStatus.OperationalStatus,
                resolvedStatus.DisplayStatus,
                model.CashierUser?.FullName,
                model.AssignedDevice?.LastSeenAt,
                model.AssignedDevice != null
            );
        }).ToList();

        var response = new TenantAdminTillListResponse(mappedItems, safePage, safePageSize, totalCount);
        return ApplicationResult<TenantAdminTillListResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminTillSummaryResponse>> GetSummaryAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(
            context,
            TenantAdminTillPermissions.View,
            TenantAdminTillPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminTillSummaryResponse>.Failure(accessError);
        }

        var response = await _repository.GetSummaryAsync(context.TenantId, cancellationToken);
        return ApplicationResult<TenantAdminTillSummaryResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminTillDetailResponse>> CreateAsync(
        TenantRequestContext context,
        TenantAdminTillCreateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(
            context,
            TenantAdminTillPermissions.Create,
            TenantAdminTillPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(accessError);
        }

        var validationError = ValidateCreateRequest(request);
        if (validationError is not null)
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(validationError);
        }

        if (!await _repository.OutletBelongsToTenantAsync(context.TenantId, request.OutletId, cancellationToken))
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(OutletNotFound);
        }

        var normalizedTillCode = TillConstants.NormalizeTillCode(request.TillCode);
        if (await _repository.TillCodeExistsForTenantAsync(
                context.TenantId,
                normalizedTillCode,
                null,
                cancellationToken))
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(new ApplicationError(
                "till.duplicate_code",
                "Till code already exists for this tenant."));
        }

        var now = _dateTimeProvider.UtcNow;
        var tillId = Guid.NewGuid();
        var actingUserId = context.UserId == Guid.Empty ? (Guid?)null : context.UserId;
        var tillAreaName = request.TillName.Trim();
        var tillNumber = await _repository.GetNextTillNumberAsync(
            context.TenantId,
            request.OutletId,
            tillAreaName,
            cancellationToken);

        var till = Till.Create(
            tillId,
            context.TenantId,
            request.OutletId,
            request.TillName,
            tillAreaName,
            tillNumber,
            normalizedTillCode,
            TillConstants.StandardTillType,
            request.DefaultOpeningFloatAmount,
            TillConstants.DefaultCurrencyCode,
            true,
            request.Status,
            actingUserId,
            now,
            request.DefaultCashierTenantUserId,
            request.DeviceName,
            request.PrinterName,
            request.ScannerName,
            request.CashDrawerName,
            request.CardReaderName,
            request.InternalNote);

        var guarded = await _resourceLimitGuard.ExecuteWithinCapacityAsync(
            context.TenantId,
            TenantSubscriptionLimitKeys.MaxTills,
            requestedIncrease: 1,
            async ct =>
            {
                await _repository.ExecuteInTransactionAsync(async () =>
                {
                    await _repository.AddAsync(till, ct);

                    if (request.PosDeviceId.HasValue)
                    {
                        var device = await _hardwareRepository.GetPosDeviceAsync(context.TenantId, request.PosDeviceId.Value, ct);
                        if (device != null)
                        {
                            var existingAssignment = await _assignmentRepository.GetActiveByTillAndDeviceAsync(context.TenantId, tillId, device.Id, ct);
                            if (existingAssignment == null)
                            {
                                var assignment = TillDeviceAssignment.Create(
                                    Guid.NewGuid(),
                                    context.TenantId,
                                    till.OutletId,
                                    tillId,
                                    device.Id,
                                    actingUserId,
                                    now);
                                await _assignmentRepository.AddAsync(assignment, ct);
                            }
                        }
                    }

                    if (request.HardwareAssignments != null && request.HardwareAssignments.Any())
                    {
                        foreach (var hw in request.HardwareAssignments)
                        {
                            var hardwareDevice = await _hardwareRepository.GetEditableDeviceAsync(context.TenantId, hw.HardwareDeviceId, ct);
                            if (hardwareDevice != null)
                            {
                                var assignment = HardwareDeviceAssignment.Create(
                                    Guid.NewGuid(),
                                    context.TenantId,
                                    hardwareDevice.OutletId,
                                    hardwareDevice.Id,
                                    tillId,
                                    null,
                                    hw.IsPrimary,
                                    actingUserId,
                                    now);
                                await _hardwareRepository.AddAssignmentAsync(assignment, ct);
                            }
                        }
                    }

                    await _repository.SaveChangesAsync(ct);
                }, ct);

                var model = await _repository.GetDetailAsync(context.TenantId, tillId, ct);
                var result = model is null
                    ? ApplicationResult<TenantAdminTillDetailResponse>.Failure(NotFound)
                    : ApplicationResult<TenantAdminTillDetailResponse>.Success(MapToDetailResponse(model));

                return result.IsSuccess
                    ? TenantResourceCapacityOperationResult<ApplicationResult<TenantAdminTillDetailResponse>>.Succeeded(result)
                    : TenantResourceCapacityOperationResult<ApplicationResult<TenantAdminTillDetailResponse>>.Aborted(result);
            },
            cancellationToken);

        if (!guarded.Allowed)
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(
                guarded.Evaluation.ToApplicationError() ??
                new ApplicationError(SubscriptionLimitErrorCodes.LimitReached, "Till subscription limit reached."));
        }

        return guarded.Value!;
    }

    public async Task<ApplicationResult<TenantAdminTillDetailResponse>> GetByIdAsync(
        TenantRequestContext context,
        Guid tillId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminTillPermissions.DetailsView,
            TenantAdminTillPermissions.View,
            TenantAdminTillPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(accessError);
        }

        var model = await _repository.GetDetailAsync(context.TenantId, tillId, cancellationToken);
        return model is null
            ? ApplicationResult<TenantAdminTillDetailResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminTillDetailResponse>.Success(MapToDetailResponse(model));
    }

    public async Task<ApplicationResult<TenantAdminTillDetailResponse>> UpdateAsync(
        TenantRequestContext context,
        Guid tillId,
        TenantAdminTillUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(
            context,
            TenantAdminTillPermissions.Update,
            TenantAdminTillPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(accessError);
        }

        var validationError = ValidateUpdateRequest(request);
        if (validationError is not null)
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(validationError);
        }

        var till = await _repository.GetEditableAsync(context.TenantId, tillId, cancellationToken);
        if (till is null)
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(NotFound);
        }

        if (!await _repository.OutletBelongsToTenantAsync(context.TenantId, request.OutletId, cancellationToken))
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(OutletNotFound);
        }

        var normalizedTillCode = TillConstants.NormalizeTillCode(request.TillCode);
        if (await _repository.TillCodeExistsForTenantAsync(
                context.TenantId,
                normalizedTillCode,
                tillId,
                cancellationToken))
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(new ApplicationError(
                "till.duplicate_code",
                "Till code already exists for this tenant."));
        }

        var actingUserId = context.UserId == Guid.Empty ? (Guid?)null : context.UserId;
        till.UpdateProfile(
            request.OutletId,
            request.TillName,
            till.TillAreaName,
            till.TillNumber,
            normalizedTillCode,
            till.TillType,
            till.DefaultOpeningFloatAmount,
            till.CurrencyCode,
            till.IsCashManaged,
            request.Status,
            actingUserId,
            _dateTimeProvider.UtcNow,
            till.DefaultCashierTenantUserId,
            request.DeviceName,
            request.PrinterName,
            request.ScannerName,
            request.CashDrawerName,
            request.CardReaderName,
            request.InternalNote);

        await _repository.SaveChangesAsync(cancellationToken);
        var model = await _repository.GetDetailAsync(context.TenantId, tillId, cancellationToken);
        return model is null
            ? ApplicationResult<TenantAdminTillDetailResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminTillDetailResponse>.Success(MapToDetailResponse(model));
    }

    public async Task<ApplicationResult> DeleteAsync(
        TenantRequestContext context,
        Guid tillId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(
            context,
            TenantAdminTillPermissions.Delete,
            TenantAdminTillPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult.Failure(accessError);
        }

        var till = await _repository.GetEditableAsync(context.TenantId, tillId, cancellationToken);
        if (till is null)
        {
            return ApplicationResult.Failure(NotFound);
        }

        if (await _repository.HasActiveSessionAsync(context.TenantId, tillId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError(
                "till.delete_conflict",
                "Till cannot be deleted while an active session is open."));
        }

        if (await _repository.HasSalesAsync(context.TenantId, tillId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError(
                "till.delete_conflict",
                "Till cannot be deleted while sales records exist."));
        }

        if (await _repository.HasCashMovementsAsync(context.TenantId, tillId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError(
                "till.delete_conflict",
                "Till cannot be deleted while cash movements exist."));
        }

        if (await _repository.HasActiveDeviceAssignmentAsync(context.TenantId, tillId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError(
                "till.delete_conflict",
                "Till cannot be deleted while a trusted device is assigned."));
        }

        var actingUserId = context.UserId == Guid.Empty ? (Guid?)null : context.UserId;
        till.SoftDelete(actingUserId, _dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult<IReadOnlyList<TenantAdminOutletOptionResponse>>> GetOutletOptionsAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminTillPermissions.OutletsView,
            TenantAdminTillPermissions.Create,
            TenantAdminTillPermissions.AssignOutlet,
            TenantAdminTillPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<IReadOnlyList<TenantAdminOutletOptionResponse>>.Failure(accessError);
        }

        var options = await _repository.GetOutletOptionsAsync(context.TenantId, cancellationToken);
        return ApplicationResult<IReadOnlyList<TenantAdminOutletOptionResponse>>.Success(options);
    }

    public async Task<ApplicationResult<TenantAdminTillCreateOptionsResponse>> GetCreateOptionsAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminTillPermissions.Create,
            TenantAdminTillPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminTillCreateOptionsResponse>.Failure(accessError);
        }

        var options = await _repository.GetCreateOptionsAsync(context.TenantId, cancellationToken);
        return ApplicationResult<TenantAdminTillCreateOptionsResponse>.Success(options);
    }

    public async Task<ApplicationResult<TenantAdminTillHardwareReadinessResponse>> GetHardwareReadinessAsync(
        TenantRequestContext context,
        Guid tillId,
        CancellationToken cancellationToken)
    {
        // Till page access remains available via till permissions on list/summary/detail.
        // Hardware readiness specifically requires tenant.hardware.view (or manage).
        var tillAccessError = ValidateAccessAny(
            context,
            TenantAdminTillPermissions.DetailsView,
            TenantAdminTillPermissions.View,
            TenantAdminTillPermissions.Manage);
        if (tillAccessError is not null)
        {
            return ApplicationResult<TenantAdminTillHardwareReadinessResponse>.Failure(tillAccessError);
        }

        var hardwareAccessError = ValidateAccess(
            context,
            TenantAdminTillPermissions.HardwareView,
            TenantAdminTillPermissions.HardwareManage);
        if (hardwareAccessError is not null)
        {
            return ApplicationResult<TenantAdminTillHardwareReadinessResponse>.Failure(
                new ApplicationError(
                    "till.permission_denied",
                    "Permission denied for hardware readiness."));
        }

        var model = await _repository.GetDetailAsync(context.TenantId, tillId, cancellationToken);
        if (model is null)
        {
            return ApplicationResult<TenantAdminTillHardwareReadinessResponse>.Failure(NotFound);
        }

        var now = _dateTimeProvider.UtcNow;
        var timeout = _options.Value.HeartbeatTimeoutSeconds;
        var tillResolved = TillMonitoringStatusResolver.Resolve(
            model.Till.Status,
            model.AssignedDevice != null,
            model.AssignedDevice?.Status,
            model.AssignedDevice?.IsTrusted ?? false,
            model.AssignedDevice?.LastSeenAt,
            now,
            timeout);

        var data = await _repository.GetHardwareReadinessDataAsync(
            context.TenantId,
            tillId,
            model.AssignedDevice?.Id,
            cancellationToken);

        var mappedConnections = data.Select(x =>
        {
            var resolved = HardwareConnectionStatusResolver.Resolve(
                x.HardwareDevice.Status,
                x.LatestTestLog?.TestStatus,
                x.LatestTestLog?.ResultMessage,
                x.LatestTestLog?.TestedAt,
                x.HardwareDevice.LastSeenAt,
                now,
                timeout);

            return new TenantAdminHardwareConnectionResponse(
                x.HardwareDevice.Id,
                x.HardwareDevice.HardwareDeviceName,
                x.HardwareDevice.HardwareDeviceType,
                x.HardwareDevice.HardwareDeviceCode,
                x.HardwareDevice.Status,
                resolved.ConnectionStatus,
                x.LatestTestLog?.TestStatus,
                x.LatestTestLog?.TestedAt,
                x.HardwareDevice.LastSeenAt,
                x.Assignment.Id,
                x.HardwareDevice.ConnectionType,
                x.HardwareDevice.Manufacturer,
                x.HardwareDevice.Model,
                resolved.HealthStatus,
                resolved.WarningCode,
                resolved.WarningMessage,
                x.Assignment.IsPrimary,
                x.AssignmentSource);
        }).ToList();

        var cashier = model.CashierUser is null
            ? null
            : new TenantAdminTillCashierResponse(
                model.CashierUser.Id,
                string.IsNullOrWhiteSpace(model.CashierUser.DisplayName)
                    ? model.CashierUser.FullName
                    : model.CashierUser.DisplayName!);

        var posDevice = model.AssignedDevice is null
            ? null
            : new TenantAdminTillPosDeviceResponse(
                model.AssignedDevice.Id,
                model.AssignedDevice.DeviceCode,
                model.AssignedDevice.DeviceName,
                model.AssignedDevice.Status,
                model.AssignedDevice.IsTrusted,
                model.AssignedDevice.LastSeenAt);

        var lastActivityAt = ResolveLastActivityAt(
            model.ActiveSession?.OpenedAt,
            model.ActiveSession?.UpdatedAt,
            model.AssignedDevice?.LastSeenAt,
            mappedConnections.Select(c => c.LastSeenAt),
            model.Till.UpdatedAt,
            model.Till.CreatedAt);

        var attentionReasons = HardwareAttentionReasonBuilder.Build(
            tillResolved.AttentionReasons,
            mappedConnections,
            model.AssignedDevice != null,
            now);

        var alertCount = HardwareAttentionReasonBuilder.CalculateAlertCount(attentionReasons);

        var response = new TenantAdminTillHardwareReadinessResponse(
            model.Till.Id,
            model.Till.TillName,
            model.Till.TillCode,
            model.Outlet.Id,
            model.Outlet.OutletName,
            mappedConnections,
            FormatStatus(model.Till.Status),
            tillResolved.DisplayStatus,
            cashier,
            lastActivityAt,
            posDevice,
            attentionReasons,
            alertCount);

        return ApplicationResult<TenantAdminTillHardwareReadinessResponse>.Success(response);
    }

    private static DateTimeOffset? ResolveLastActivityAt(
        DateTimeOffset? sessionOpenedAt,
        DateTimeOffset? sessionUpdatedAt,
        DateTimeOffset? posLastSeenAt,
        IEnumerable<DateTimeOffset?> hardwareLastSeenAt,
        DateTimeOffset? tillUpdatedAt,
        DateTimeOffset tillCreatedAt)
    {
        DateTimeOffset? max = null;
        void Consider(DateTimeOffset? value)
        {
            if (!value.HasValue)
            {
                return;
            }

            if (!max.HasValue || value.Value > max.Value)
            {
                max = value;
            }
        }

        Consider(sessionOpenedAt);
        Consider(sessionUpdatedAt);
        Consider(posLastSeenAt);
        foreach (var hardwareSeen in hardwareLastSeenAt)
        {
            Consider(hardwareSeen);
        }

        Consider(tillUpdatedAt);
        Consider(tillCreatedAt);
        return max;
    }

    private static ApplicationError? ValidateCreateRequest(TenantAdminTillCreateRequest request)
    {
        return ValidateWriteRequest(
            request.OutletId,
            request.TillName,
            request.TillCode,
            request.Status,
            request.DeviceName,
            request.PrinterName,
            request.ScannerName,
            request.CashDrawerName,
            request.CardReaderName,
            request.InternalNote);
    }

    private static ApplicationError? ValidateUpdateRequest(TenantAdminTillUpdateRequest request)
    {
        return ValidateWriteRequest(
            request.OutletId,
            request.TillName,
            request.TillCode,
            request.Status,
            request.DeviceName,
            request.PrinterName,
            request.ScannerName,
            request.CashDrawerName,
            request.CardReaderName,
            request.InternalNote);
    }

    private static ApplicationError? ValidateWriteRequest(
        Guid outletId,
        string tillName,
        string tillCode,
        string status,
        string? deviceName,
        string? printerName,
        string? scannerName,
        string? cashDrawerName,
        string? cardReaderName,
        string? internalNote)
    {
        if (outletId == Guid.Empty)
        {
            return ValidationFailed("Outlet is required.");
        }

        if (string.IsNullOrWhiteSpace(tillName) || tillName.Trim().Length > 120)
        {
            return ValidationFailed("Till name is required and must be 120 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(tillCode) || tillCode.Trim().Length > 40)
        {
            return ValidationFailed("Till code is required and must be 40 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(status) || !TillConstants.IsValidWriteStatus(status))
        {
            return ValidationFailed("Till status must be Active, Inactive, or Maintenance.");
        }

        if (IsTooLong(deviceName, 120) ||
            IsTooLong(printerName, 120) ||
            IsTooLong(scannerName, 120) ||
            IsTooLong(cashDrawerName, 120) ||
            IsTooLong(cardReaderName, 120) ||
            IsTooLong(internalNote, 500))
        {
            return ValidationFailed("One or more hardware fields exceed the maximum length.");
        }

        return null;
    }

    private static bool IsTooLong(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength;

    private static ApplicationError ValidationFailed(string message) =>
        new("till.validation_failed", message);

    private static ApplicationError? ValidateAccess(
        TenantRequestContext context,
        string requiredPermission,
        string managePermission)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("till.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(requiredPermission) || context.HasPermission(managePermission)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateAccessAny(
        TenantRequestContext context,
        params string[] permissions)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("till.invalid_tenant_context", "Invalid tenant context.");
        }

        return permissions.Any(context.HasPermission) ? null : PermissionDenied;
    }

    private TenantAdminTillDetailResponse MapToDetailResponse(TillMonitoringReadModel model)
    {
        var now = _dateTimeProvider.UtcNow;
        var timeout = _options.Value.HeartbeatTimeoutSeconds;

        var resolvedStatus = TillMonitoringStatusResolver.Resolve(
            model.Till.Status,
            model.AssignedDevice != null,
            model.AssignedDevice?.Status,
            model.AssignedDevice?.IsTrusted ?? false,
            model.AssignedDevice?.LastSeenAt,
            now,
            timeout
        );

        return new TenantAdminTillDetailResponse(
            model.Till.Id,
            model.Till.TillName,
            model.Till.TillCode,
            model.Outlet.Id,
            model.Outlet.OutletName,
            model.Outlet.OutletCode,
            FormatStatus(model.Till.Status),
            model.AssignedDevice?.Status,
            model.Till.UpdatedAt ?? model.Till.CreatedAt,
            resolvedStatus.NeedsAttention,
            resolvedStatus.OperationalStatus,
            resolvedStatus.DisplayStatus,
            model.CashierUser?.FullName,
            model.AssignedDevice?.LastSeenAt,
            model.AssignedDevice != null,
            model.Till.DeviceName,
            model.Till.PrinterName,
            model.Till.ScannerName,
            model.Till.CashDrawerName,
            model.Till.CardReaderName,
            model.Till.InternalNote,
            model.Till.DefaultOpeningFloatAmount,
            model.Till.CurrencyCode,
            null, // DefaultCashier
            null, // PosDevice
            null, // HardwareAssignments
            model.Till.CreatedAt,
            model.Till.UpdatedAt ?? model.Till.CreatedAt
        );
    }

    private static string FormatStatus(string status)
    {
        return status.Trim().ToUpperInvariant() switch
        {
            TillConstants.ActiveStatus => "Active",
            TillConstants.InactiveStatus => "Inactive",
            TillConstants.MaintenanceStatus => "Maintenance",
            _ => status,
        };
    }
}
