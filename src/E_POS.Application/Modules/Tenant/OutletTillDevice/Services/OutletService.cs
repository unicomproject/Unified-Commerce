using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Services;

public sealed class OutletService : IOutletService
{
    private const string OutletCodePrefix = "OUT";
    private const string OutletCodeSequenceKey = "OUTLET_CODE";
    private const int GeneratedCodePaddingLength = 3;
    private const int MaxCodeGenerationAttempts = 5;
    private static readonly ApplicationError PermissionDenied = new("outlet.permission_denied", "Permission denied for outlet management.");
    private static readonly ApplicationError FeatureDisabled = new("outlet.feature_disabled", "Outlet management is not enabled for this tenant.");
    private static readonly ApplicationError ClickCollectFeatureDisabled = new("outlet.click_collect_feature_disabled", "Click & collect is not enabled for this tenant.");
    private static readonly ApplicationError TenantBlocked = new("outlet.tenant_blocked", "Tenant status does not allow outlet management.");
    private static readonly ApplicationError NotFound = new("outlet.not_found", "Outlet was not found.");
    private readonly IOutletRepository _repository;
    private readonly ICodeSequenceRepository _codeSequenceRepository;
    private readonly IOutletRequestValidator _requestValidator;
    private readonly IOutletAuditLogger _auditLogger;
    private readonly IDateTimeProvider _dateTimeProvider;

    public OutletService(
        IOutletRepository repository,
        ICodeSequenceRepository codeSequenceRepository,
        IOutletRequestValidator requestValidator,
        IOutletAuditLogger auditLogger,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _codeSequenceRepository = codeSequenceRepository;
        _requestValidator = requestValidator;
        _auditLogger = auditLogger;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<OutletCreateOptionsResponse>> GetCreateOptionsAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateManageAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<OutletCreateOptionsResponse>.Failure(accessError);
        }

        var operationalError = await ValidateOperationalAccessAsync(context, cancellationToken);
        if (operationalError is not null)
        {
            return ApplicationResult<OutletCreateOptionsResponse>.Failure(operationalError);
        }

        var response = await _repository.GetCreateOptionsAsync(context.TenantId, cancellationToken);
        return ApplicationResult<OutletCreateOptionsResponse>.Success(response);
    }

    public async Task<ApplicationResult<OutletResponse>> CreateAsync(TenantRequestContext context, OutletCreateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateManageAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<OutletResponse>.Failure(accessError);
        }

        var operationalError = await ValidateOperationalAccessAsync(context, cancellationToken);
        if (operationalError is not null)
        {
            return ApplicationResult<OutletResponse>.Failure(operationalError);
        }

        var validationError = _requestValidator.ValidateCreate(request);
        if (validationError is not null)
        {
            return ApplicationResult<OutletResponse>.Failure(validationError);
        }

        if (request.CollectionEnabled &&
            !await _repository.IsClickCollectFeatureEnabledAsync(
                context.TenantId,
                _dateTimeProvider.UtcNow,
                cancellationToken))
        {
            return ApplicationResult<OutletResponse>.Failure(ClickCollectFeatureDisabled);
        }

        for (var attempt = 0; attempt < MaxCodeGenerationAttempts; attempt++)
        {
            var now = _dateTimeProvider.UtcNow;
            var outletCode = await GenerateOutletCodeAsync(context.TenantId, now, cancellationToken);
            if (await _repository.OutletCodeExistsAsync(context.TenantId, outletCode, null, cancellationToken))
            {
                continue;
            }

            var outletId = Guid.NewGuid();
            var outlet = Outlet.Create(
                outletId,
                context.TenantId,
                request.OutletName,
                outletCode,
                request.Status,
                request.OutletType,
                request.Timezone,
                request.IsDefaultOutlet,
                request.Phone,
                request.Email,
                context.UserId,
                now);
            var address = CreateAddress(context.TenantId, outletId, request.Address, context.UserId, now);
            var hours = CreateBusinessHours(context.TenantId, outletId, request.BusinessHours, now);
            var pickupMapping = await CreatePickupMappingAsync(
                context.TenantId,
                outletId,
                request.CollectionEnabled,
                request.PreparationLeadMinutes,
                request.PickupWindowMinutes,
                request.CollectionCutoffTime,
                now,
                cancellationToken);
            if (pickupMapping.Error is not null)
            {
                return ApplicationResult<OutletResponse>.Failure(pickupMapping.Error);
            }

            if (!await _repository.AddAsync(outlet, address, hours, pickupMapping.Value, cancellationToken))
            {
                continue;
            }

            _auditLogger.LogOutletCreated(
                context.TenantId,
                context.UserId,
                outletId,
                outlet.OutletCode,
                outlet.OutletType,
                outlet.Status);

            var response = await _repository.GetByIdAsync(context.TenantId, outletId, false, cancellationToken);
            return ApplicationResult<OutletResponse>.Success(response!);
        }

        return ApplicationResult<OutletResponse>.Failure(CreateDuplicateCodeError());
    }

    public async Task<ApplicationResult<OutletListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var accessError = ValidateReadAccess(context);
        if (accessError is not null) return ApplicationResult<OutletListResponse>.Failure(accessError);

        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var response = await _repository.ListAsync(context.TenantId, safePageNumber, safePageSize, search, cancellationToken);
        return ApplicationResult<OutletListResponse>.Success(response);
    }

    public async Task<ApplicationResult<OutletResponse>> GetByIdAsync(TenantRequestContext context, Guid outletId, CancellationToken cancellationToken)
    {
        var accessError = ValidateReadAccess(context);
        if (accessError is not null) return ApplicationResult<OutletResponse>.Failure(accessError);

        var response = await _repository.GetByIdAsync(context.TenantId, outletId, false, cancellationToken);
        return response is null ? ApplicationResult<OutletResponse>.Failure(NotFound) : ApplicationResult<OutletResponse>.Success(response);
    }

    public async Task<ApplicationResult<OutletResponse>> UpdateAsync(TenantRequestContext context, Guid outletId, OutletUpdateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateManageAccess(context);
        if (accessError is not null) return ApplicationResult<OutletResponse>.Failure(accessError);

        var operationalError = await ValidateOperationalAccessAsync(context, cancellationToken);
        if (operationalError is not null)
        {
            return ApplicationResult<OutletResponse>.Failure(operationalError);
        }

        var validationError = _requestValidator.ValidateUpdate(request);
        if (validationError is not null) return ApplicationResult<OutletResponse>.Failure(validationError);

        if (request.CollectionEnabled &&
            !await _repository.IsClickCollectFeatureEnabledAsync(
                context.TenantId,
                _dateTimeProvider.UtcNow,
                cancellationToken))
        {
            return ApplicationResult<OutletResponse>.Failure(ClickCollectFeatureDisabled);
        }

        var aggregate = await _repository.GetEditAggregateAsync(context.TenantId, outletId, cancellationToken);
        if (aggregate is null) return ApplicationResult<OutletResponse>.Failure(NotFound);

        var normalizedOutletCode = aggregate.Outlet.OutletCode;
        if (await _repository.OutletCodeExistsAsync(context.TenantId, normalizedOutletCode, outletId, cancellationToken))
        {
            return ApplicationResult<OutletResponse>.Failure(CreateDuplicateCodeError());
        }

        var status = OutletConstants.NormalizeStatus(request.Status);
        if (status == OutletConstants.DeletedStatus && await _repository.HasActiveTillOrDeviceAsync(context.TenantId, outletId, cancellationToken))
        {
            return ApplicationResult<OutletResponse>.Failure(CreateDeleteConflict());
        }

        var now = _dateTimeProvider.UtcNow;
        aggregate.Outlet.UpdateProfile(
            request.OutletName,
            normalizedOutletCode,
            request.Status,
            request.OutletType,
            request.Timezone,
            request.IsDefaultOutlet,
            request.Phone,
            request.Email,
            context.UserId,
            now);
        var address = UpdateOrCreateAddress(context.TenantId, aggregate.PhysicalAddress, outletId, request.Address, context.UserId, now);
        var hours = CreateBusinessHours(context.TenantId, outletId, request.BusinessHours, now);
        var enableCollection = aggregate.Outlet.Status != OutletConstants.DeletedStatus && request.CollectionEnabled;
        var pickupMapping = await UpdatePickupMappingAsync(
            context.TenantId,
            outletId,
            aggregate.PickupMapping,
            enableCollection,
            request.PreparationLeadMinutes,
            request.PickupWindowMinutes,
            request.CollectionCutoffTime,
            now,
            cancellationToken);
        if (pickupMapping.Error is not null) return ApplicationResult<OutletResponse>.Failure(pickupMapping.Error);

        if (!await _repository.SaveUpdatedAsync(aggregate, address, hours, pickupMapping.Value, cancellationToken))
        {
            return ApplicationResult<OutletResponse>.Failure(CreateDuplicateCodeError());
        }

        var includeDeleted = aggregate.Outlet.Status == OutletConstants.DeletedStatus;
        var response = await _repository.GetByIdAsync(context.TenantId, outletId, includeDeleted, cancellationToken);
        return response is null ? ApplicationResult<OutletResponse>.Failure(NotFound) : ApplicationResult<OutletResponse>.Success(response);
    }

    public async Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid outletId, CancellationToken cancellationToken)
    {
        var accessError = ValidateManageAccess(context);
        if (accessError is not null) return ApplicationResult.Failure(accessError);

        var aggregate = await _repository.GetEditAggregateAsync(context.TenantId, outletId, cancellationToken);
        if (aggregate is null) return ApplicationResult.Failure(NotFound);

        if (await _repository.HasActiveTillOrDeviceAsync(context.TenantId, outletId, cancellationToken))
        {
            return ApplicationResult.Failure(CreateDeleteConflict());
        }

        var now = _dateTimeProvider.UtcNow;
        aggregate.Outlet.SoftDelete(context.UserId, now);
        aggregate.PickupMapping?.SetStatus(OutletConstants.InactiveStatus, now);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task<ApplicationError?> ValidateOperationalAccessAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var tenantStatus = await _repository.GetTenantStatusAsync(context.TenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(tenantStatus) || !TenantAuthConstants.IsTenantLoginStatusAllowed(tenantStatus))
        {
            return TenantBlocked;
        }

        if (!await _repository.IsOutletManagementFeatureEnabledAsync(context.TenantId, cancellationToken))
        {
            return FeatureDisabled;
        }

        return null;
    }

    private static ApplicationError? ValidateReadAccess(TenantRequestContext context)
    {
        var contextError = ValidateTenantContext(context);
        if (contextError is not null)
        {
            return contextError;
        }

        return context.HasPermission(OutletConstants.ViewPermission) || context.HasPermission(OutletConstants.ManagePermission)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateManageAccess(TenantRequestContext context)
    {
        var contextError = ValidateTenantContext(context);
        if (contextError is not null)
        {
            return contextError;
        }

        return context.HasPermission(OutletConstants.ManagePermission) ? null : PermissionDenied;
    }

    private static ApplicationError? ValidateTenantContext(TenantRequestContext context)
    {
        return context.TenantId == Guid.Empty || context.UserId == Guid.Empty
            ? new ApplicationError("outlet.invalid_tenant_context", "Invalid tenant context.")
            : null;
    }

    private async Task<PickupMappingResult> CreatePickupMappingAsync(
        Guid tenantId,
        Guid outletId,
        bool collectionEnabled,
        int? preparationLeadMinutes,
        int? pickupWindowMinutes,
        TimeOnly? collectionCutoffTime,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!collectionEnabled) return new PickupMappingResult(null, null);

        var pickupMethodId = await _repository.GetActivePickupFulfillmentMethodIdAsync(tenantId, cancellationToken);
        if (pickupMethodId is null)
        {
            return new PickupMappingResult(null, new ApplicationError("outlet.pickup_method_missing", "Active pickup fulfillment method is required before enabling collection point."));
        }

        return new PickupMappingResult(
            FulfillmentMethodOutlet.Create(
                Guid.NewGuid(),
                tenantId,
                pickupMethodId.Value,
                outletId,
                preparationLeadMinutes,
                pickupWindowMinutes,
                collectionCutoffTime,
                OutletConstants.ActiveStatus,
                now),
            null);
    }

    private async Task<PickupMappingResult> UpdatePickupMappingAsync(
        Guid tenantId,
        Guid outletId,
        FulfillmentMethodOutlet? existingMapping,
        bool collectionEnabled,
        int? preparationLeadMinutes,
        int? pickupWindowMinutes,
        TimeOnly? collectionCutoffTime,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!collectionEnabled)
        {
            existingMapping?.ConfigureCollection(
                preparationLeadMinutes,
                pickupWindowMinutes,
                collectionCutoffTime,
                OutletConstants.InactiveStatus,
                now);
            return new PickupMappingResult(null, null);
        }

        if (existingMapping is not null)
        {
            existingMapping.ConfigureCollection(
                preparationLeadMinutes,
                pickupWindowMinutes,
                collectionCutoffTime,
                OutletConstants.ActiveStatus,
                now);
            return new PickupMappingResult(null, null);
        }

        return await CreatePickupMappingAsync(
            tenantId,
            outletId,
            collectionEnabled,
            preparationLeadMinutes,
            pickupWindowMinutes,
            collectionCutoffTime,
            now,
            cancellationToken);
    }

    private Task<string> GenerateOutletCodeAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return _codeSequenceRepository.GetNextCodeAsync(tenantId, OutletCodeSequenceKey, OutletCodePrefix, GeneratedCodePaddingLength, now, cancellationToken);
    }

    private static OutletAddress CreateAddress(Guid tenantId, Guid outletId, OutletAddressRequest request, Guid? createdByTenantUserId, DateTimeOffset now) =>
        OutletAddress.Create(
            Guid.NewGuid(),
            tenantId,
            outletId,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.StateOrProvince,
            request.PostalCode,
            request.CountryCode,
            request.ContactName,
            request.ContactPhone,
            createdByTenantUserId,
            now);

    private static OutletAddress UpdateOrCreateAddress(Guid tenantId, OutletAddress? currentAddress, Guid outletId, OutletAddressRequest request, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        if (currentAddress is null) return CreateAddress(tenantId, outletId, request, updatedByTenantUserId, now);
        currentAddress.UpdatePhysicalAddress(
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.StateOrProvince,
            request.PostalCode,
            request.CountryCode,
            request.ContactName,
            request.ContactPhone,
            updatedByTenantUserId,
            now);
        return currentAddress;
    }

    private static IReadOnlyList<OutletBusinessHour> CreateBusinessHours(Guid tenantId, Guid outletId, IReadOnlyList<OutletBusinessHourRequest>? request, DateTimeOffset now)
    {
        return (request ?? [])
            .OrderBy(x => x.DayOfWeek)
            .Select(x => OutletBusinessHour.Create(
                Guid.NewGuid(),
                tenantId,
                outletId,
                (short)x.DayOfWeek,
                x.OpeningTime,
                x.ClosingTime,
                x.IsClosed,
                x.ValidFrom,
                x.ValidUntil,
                now))
            .ToList();
    }

    private static ApplicationError CreateDuplicateCodeError() =>
        new(
            "outlet.duplicate_code",
            "Outlet code already exists for this tenant.",
            [new ApplicationFieldError("outletCode", "Outlet code already exists for this tenant.")]);

    private static ApplicationError CreateDeleteConflict() => new("outlet.delete_conflict", "Outlet cannot be deleted while active tills or POS devices are assigned.");
    private sealed record PickupMappingResult(FulfillmentMethodOutlet? Value, ApplicationError? Error);
}
