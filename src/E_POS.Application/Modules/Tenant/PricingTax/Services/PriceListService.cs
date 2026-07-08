using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Domain.Modules.Tenant.PricingTax.Constants;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;

namespace E_POS.Application.Modules.Tenant.PricingTax.Services;

public sealed class PriceListService : IPriceListService
{
    private static readonly ApplicationError PermissionDenied = new("pricing.price_list.permission_denied", "Permission denied for pricing management.");
    private static readonly ApplicationError NotFound = new("pricing.price_list.not_found", "Price list was not found.");
    
    private readonly IPriceListRepository _repository;
    private readonly IOutletRepository _outletRepository;
    private readonly ITenantLookupRepository _tenantLookupRepository;
    private readonly IPriceListRequestValidator _validator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PriceListService(
        IPriceListRepository repository,
        IOutletRepository outletRepository,
        ITenantLookupRepository tenantLookupRepository,
        IPriceListRequestValidator validator,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _outletRepository = outletRepository;
        _tenantLookupRepository = tenantLookupRepository;
        _validator = validator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<PriceListResponse>> CreateAsync(
        TenantRequestContext context,
        PriceListCreateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxConstants.CreatePermission);
        if (accessError is not null) return ApplicationResult<PriceListResponse>.Failure(accessError);

        var validationError = _validator.ValidateCreate(request);
        if (validationError is not null) return ApplicationResult<PriceListResponse>.Failure(validationError);

        var normalizedCode = PricingTaxConstants.NormalizeCode(request.PriceListCode);
        if (await _repository.PriceListCodeExistsAsync(context.TenantId, normalizedCode, null, cancellationToken))
        {
            return ApplicationResult<PriceListResponse>.Failure(new ApplicationError("pricing.price_list.duplicate_code", "Price list code already exists."));
        }

        if (!await _tenantLookupRepository.CurrencyExistsAsync(request.CurrencyCode, cancellationToken))
        {
            return ApplicationResult<PriceListResponse>.Failure(new ApplicationError("pricing.price_list.invalid_currency", "Currency code is invalid."));
        }

        if (request.AssignedOutletIds != null && request.AssignedOutletIds.Length > 0)
        {
            if (!await _outletRepository.AllOutletsBelongToTenantAsync(context.TenantId, request.AssignedOutletIds, cancellationToken))
            {
                return ApplicationResult<PriceListResponse>.Failure(new ApplicationError("pricing.price_list.invalid_outlets", "One or more assigned outlets are invalid."));
            }
        }

        if (request.AssignedSalesChannelIds != null && request.AssignedSalesChannelIds.Length > 0)
        {
            if (!await _tenantLookupRepository.AllSalesChannelsExistAsync(context.TenantId, request.AssignedSalesChannelIds, cancellationToken))
            {
                return ApplicationResult<PriceListResponse>.Failure(new ApplicationError("pricing.price_list.invalid_channels", "One or more assigned sales channels are invalid."));
            }
        }

        var priceListId = Guid.NewGuid();
        var now = _dateTimeProvider.UtcNow;

        var priceList = PriceList.Create(
            id: priceListId,
            tenantId: context.TenantId,
            priceListCode: normalizedCode,
            priceListName: request.PriceListName,
            priceListType: request.PriceListType,
            currencyCode: request.CurrencyCode,
            priceIncludesTax: request.PriceIncludesTax,
            isDefaultPriceList: request.IsDefaultPriceList,
            priority: request.Priority,
            validFrom: request.ValidFrom,
            validUntil: request.ValidUntil,
            status: request.Status,
            createdByTenantUserId: context.UserId,
            now: now);

        if (priceList.IsDefaultPriceList && priceList.Status == PricingTaxConstants.ActiveStatus)
        {
            await _repository.ClearOtherDefaultsAsync(context.TenantId, priceListId, cancellationToken);
        }

        await _repository.AddAsync(priceList, cancellationToken);

        if (request.AssignedOutletIds != null && request.AssignedOutletIds.Length > 0)
        {
            var outletAssignments = request.AssignedOutletIds.Select(outletId =>
                PriceListOutlet.Create(Guid.NewGuid(), context.TenantId, priceListId, outletId, PricingTaxConstants.ActiveStatus, context.UserId, now));
            await _repository.AddOutletAssignmentsAsync(outletAssignments, cancellationToken);
        }

        if (request.AssignedSalesChannelIds != null && request.AssignedSalesChannelIds.Length > 0)
        {
            var channelAssignments = request.AssignedSalesChannelIds.Select(channelId =>
                PriceListChannel.Create(Guid.NewGuid(), context.TenantId, priceListId, channelId, PricingTaxConstants.ActiveStatus, context.UserId, now));
            await _repository.AddChannelAssignmentsAsync(channelAssignments, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        var response = await _repository.GetByIdAsync(context.TenantId, priceListId, cancellationToken);
        return ApplicationResult<PriceListResponse>.Success(response!);
    }

    public async Task<ApplicationResult<PriceListListResponse>> ListAsync(
        TenantRequestContext context,
        int pageNumber,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<PriceListListResponse>.Failure(accessError);

        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var response = await _repository.ListAsync(context.TenantId, safePageNumber, safePageSize, search, cancellationToken);
        return ApplicationResult<PriceListListResponse>.Success(response);
    }

    public async Task<ApplicationResult<PriceListResponse>> GetByIdAsync(
        TenantRequestContext context,
        Guid id,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<PriceListResponse>.Failure(accessError);

        var response = await _repository.GetByIdAsync(context.TenantId, id, cancellationToken);
        if (response is null) return ApplicationResult<PriceListResponse>.Failure(NotFound);

        return ApplicationResult<PriceListResponse>.Success(response);
    }

    public async Task<ApplicationResult<PriceListResponse>> UpdateAsync(
        TenantRequestContext context,
        Guid id,
        PriceListUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxConstants.UpdatePermission);
        if (accessError is not null) return ApplicationResult<PriceListResponse>.Failure(accessError);

        var validationError = _validator.ValidateUpdate(request);
        if (validationError is not null) return ApplicationResult<PriceListResponse>.Failure(validationError);

        var priceList = await _repository.GetEditableAsync(context.TenantId, id, cancellationToken);
        if (priceList is null) return ApplicationResult<PriceListResponse>.Failure(NotFound);

        var normalizedCode = PricingTaxConstants.NormalizeCode(request.PriceListCode);
        if (await _repository.PriceListCodeExistsAsync(context.TenantId, normalizedCode, id, cancellationToken))
        {
            return ApplicationResult<PriceListResponse>.Failure(new ApplicationError("pricing.price_list.duplicate_code", "Price list code already exists."));
        }

        if (!await _tenantLookupRepository.CurrencyExistsAsync(request.CurrencyCode, cancellationToken))
        {
            return ApplicationResult<PriceListResponse>.Failure(new ApplicationError("pricing.price_list.invalid_currency", "Currency code is invalid."));
        }

        if (request.AssignedOutletIds != null && request.AssignedOutletIds.Length > 0)
        {
            if (!await _outletRepository.AllOutletsBelongToTenantAsync(context.TenantId, request.AssignedOutletIds, cancellationToken))
            {
                return ApplicationResult<PriceListResponse>.Failure(new ApplicationError("pricing.price_list.invalid_outlets", "One or more assigned outlets are invalid."));
            }
        }

        if (request.AssignedSalesChannelIds != null && request.AssignedSalesChannelIds.Length > 0)
        {
            if (!await _tenantLookupRepository.AllSalesChannelsExistAsync(context.TenantId, request.AssignedSalesChannelIds, cancellationToken))
            {
                return ApplicationResult<PriceListResponse>.Failure(new ApplicationError("pricing.price_list.invalid_channels", "One or more assigned sales channels are invalid."));
            }
        }

        var now = _dateTimeProvider.UtcNow;

        priceList.UpdateProfile(
            priceListCode: normalizedCode,
            priceListName: request.PriceListName,
            priceListType: request.PriceListType,
            currencyCode: request.CurrencyCode,
            priceIncludesTax: request.PriceIncludesTax,
            isDefaultPriceList: request.IsDefaultPriceList,
            priority: request.Priority,
            validFrom: request.ValidFrom,
            validUntil: request.ValidUntil,
            status: request.Status,
            updatedByTenantUserId: context.UserId,
            now: now);

        if (priceList.IsDefaultPriceList && priceList.Status == PricingTaxConstants.ActiveStatus)
        {
            await _repository.ClearOtherDefaultsAsync(context.TenantId, id, cancellationToken);
        }

        await _repository.ClearAssignmentsAsync(id, cancellationToken);

        if (request.AssignedOutletIds != null && request.AssignedOutletIds.Length > 0)
        {
            var outletAssignments = request.AssignedOutletIds.Select(outletId =>
                PriceListOutlet.Create(Guid.NewGuid(), context.TenantId, id, outletId, PricingTaxConstants.ActiveStatus, context.UserId, now));
            await _repository.AddOutletAssignmentsAsync(outletAssignments, cancellationToken);
        }

        if (request.AssignedSalesChannelIds != null && request.AssignedSalesChannelIds.Length > 0)
        {
            var channelAssignments = request.AssignedSalesChannelIds.Select(channelId =>
                PriceListChannel.Create(Guid.NewGuid(), context.TenantId, id, channelId, PricingTaxConstants.ActiveStatus, context.UserId, now));
            await _repository.AddChannelAssignmentsAsync(channelAssignments, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        var response = await _repository.GetByIdAsync(context.TenantId, id, cancellationToken);
        return ApplicationResult<PriceListResponse>.Success(response!);
    }

    public async Task<ApplicationResult<Guid>> DeleteAsync(
        TenantRequestContext context,
        Guid id,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxConstants.DeletePermission);
        if (accessError is not null) return ApplicationResult<Guid>.Failure(accessError);

        var priceList = await _repository.GetEditableAsync(context.TenantId, id, cancellationToken);
        if (priceList is null) return ApplicationResult<Guid>.Failure(NotFound);

        var now = _dateTimeProvider.UtcNow;
        priceList.SoftDelete(context.UserId, now);

        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(id);
    }

    private static ApplicationError? ValidateAccess(TenantRequestContext context, string requiredPermission)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("pricing.price_list.invalid_tenant_context", "Invalid tenant context.");
        }
        if (!context.HasPermission(requiredPermission) && !context.HasPermission(PricingTaxConstants.ManagePermission))
        {
            return PermissionDenied;
        }
        return null;
    }
}


