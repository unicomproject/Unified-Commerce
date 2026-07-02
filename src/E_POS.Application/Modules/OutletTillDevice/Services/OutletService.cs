using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.OutletTillDevice.Contracts;
using E_POS.Application.Modules.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Constants;
using E_POS.Domain.Modules.OutletTillDevice.Entities;

namespace E_POS.Application.Modules.OutletTillDevice.Services;

public sealed class OutletService : IOutletService
{
    private const string OutletCodePrefix = "OUT";
    private const string OutletCodeSequenceKey = "OUTLET_CODE";
    private const int GeneratedCodePaddingLength = 3;
    private const int MaxCodeGenerationAttempts = 5;
    private static readonly ApplicationError PermissionDenied = new("outlet.permission_denied", "Permission denied for outlet management.");
    private static readonly ApplicationError NotFound = new("outlet.not_found", "Outlet was not found.");
    private readonly IOutletRepository _repository;
    private readonly ICodeSequenceRepository _codeSequenceRepository;
    private readonly IOutletRequestValidator _requestValidator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public OutletService(IOutletRepository repository, ICodeSequenceRepository codeSequenceRepository, IOutletRequestValidator requestValidator, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _codeSequenceRepository = codeSequenceRepository;
        _requestValidator = requestValidator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<OutletResponse>> CreateAsync(TenantRequestContext context, OutletCreateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateManageAccess(context);
        if (accessError is not null) return ApplicationResult<OutletResponse>.Failure(accessError);

        var validationError = _requestValidator.ValidateCreate(request);
        if (validationError is not null) return ApplicationResult<OutletResponse>.Failure(validationError);

        for (var attempt = 0; attempt < MaxCodeGenerationAttempts; attempt++)
        {
            var now = _dateTimeProvider.UtcNow;
            var outletCode = await GenerateOutletCodeAsync(context.TenantId, now, cancellationToken);
            if (await _repository.OutletCodeExistsAsync(context.TenantId, outletCode, null, cancellationToken))
            {
                continue;
            }

            var outletId = Guid.NewGuid();
            var outlet = Outlet.Create(outletId, context.TenantId, request.Name, outletCode, request.Status, request.OutletType, request.IsOnlineVisible, request.ContactPhone, request.ContactEmail, now);
            var address = CreateAddress(outletId, request.Address, now);
            var hours = CreateBusinessHours(outletId, request.BusinessHours, now);
            var pickupMapping = await CreatePickupMappingAsync(context.TenantId, outletId, request.CollectionEnabled, now, cancellationToken);
            if (pickupMapping.Error is not null) return ApplicationResult<OutletResponse>.Failure(pickupMapping.Error);

            if (!await _repository.AddAsync(outlet, address, hours, pickupMapping.Value, cancellationToken))
            {
                continue;
            }

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

        var validationError = _requestValidator.ValidateUpdate(request);
        if (validationError is not null) return ApplicationResult<OutletResponse>.Failure(validationError);

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
        aggregate.Outlet.UpdateProfile(request.Name, normalizedOutletCode, request.Status, request.OutletType, request.IsOnlineVisible, request.ContactPhone, request.ContactEmail, now);
        var address = UpdateOrCreateAddress(aggregate.PhysicalAddress, outletId, request.Address, now);
        var hours = CreateBusinessHours(outletId, request.BusinessHours, now);
        var enableCollection = aggregate.Outlet.Status != OutletConstants.DeletedStatus && request.CollectionEnabled;
        var pickupMapping = await UpdatePickupMappingAsync(context.TenantId, outletId, aggregate.PickupMapping, enableCollection, now, cancellationToken);
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
        aggregate.Outlet.SoftDelete(now);
        aggregate.PickupMapping?.SetStatus(OutletConstants.InactiveStatus, now);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
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

    private async Task<PickupMappingResult> CreatePickupMappingAsync(Guid tenantId, Guid outletId, bool collectionEnabled, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!collectionEnabled) return new PickupMappingResult(null, null);

        var pickupMethodId = await _repository.GetActivePickupFulfillmentMethodIdAsync(tenantId, cancellationToken);
        if (pickupMethodId is null)
        {
            return new PickupMappingResult(null, new ApplicationError("outlet.pickup_method_missing", "Active pickup fulfillment method is required before enabling collection point."));
        }

        return new PickupMappingResult(FulfillmentMethodOutlet.Create(Guid.NewGuid(), pickupMethodId.Value, outletId, OutletConstants.ActiveStatus, now), null);
    }

    private async Task<PickupMappingResult> UpdatePickupMappingAsync(Guid tenantId, Guid outletId, FulfillmentMethodOutlet? existingMapping, bool collectionEnabled, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!collectionEnabled)
        {
            existingMapping?.SetStatus(OutletConstants.InactiveStatus, now);
            return new PickupMappingResult(null, null);
        }

        if (existingMapping is not null)
        {
            existingMapping.SetStatus(OutletConstants.ActiveStatus, now);
            return new PickupMappingResult(null, null);
        }

        return await CreatePickupMappingAsync(tenantId, outletId, collectionEnabled, now, cancellationToken);
    }

    private Task<string> GenerateOutletCodeAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return _codeSequenceRepository.GetNextCodeAsync(tenantId, OutletCodeSequenceKey, OutletCodePrefix, GeneratedCodePaddingLength, now, cancellationToken);
    }


    private static OutletAddress CreateAddress(Guid outletId, OutletAddressRequest request, DateTimeOffset now) => OutletAddress.Create(Guid.NewGuid(), outletId, request.AddressLine1, request.AddressLine2, request.City, request.StateOrProvince, request.PostalCode, request.CountryCode, now);

    private static OutletAddress UpdateOrCreateAddress(OutletAddress? currentAddress, Guid outletId, OutletAddressRequest request, DateTimeOffset now)
    {
        if (currentAddress is null) return CreateAddress(outletId, request, now);
        currentAddress.UpdatePhysicalAddress(request.AddressLine1, request.AddressLine2, request.City, request.StateOrProvince, request.PostalCode, request.CountryCode, now);
        return currentAddress;
    }

    private static IReadOnlyList<OutletBusinessHour> CreateBusinessHours(Guid outletId, IReadOnlyList<OutletBusinessHourRequest>? request, DateTimeOffset now)
    {
        return (request ?? []).OrderBy(x => x.DayOfWeek).Select(x => OutletBusinessHour.Create(Guid.NewGuid(), outletId, x.DayOfWeek, x.OpenTime, x.CloseTime, now)).ToList();
    }

    private static ApplicationError CreateDuplicateCodeError() => new("outlet.duplicate_code", "Outlet code already exists for this tenant.");
    private static ApplicationError CreateDeleteConflict() => new("outlet.delete_conflict", "Outlet cannot be deleted while active tills or POS devices are assigned.");
    private sealed record PickupMappingResult(FulfillmentMethodOutlet? Value, ApplicationError? Error);
}
