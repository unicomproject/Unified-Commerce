using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.PricingTax.Contracts;
using E_POS.Application.Modules.PricingTax.Dtos;
using E_POS.Domain.Modules.PricingTax.Constants;
using E_POS.Domain.Modules.PricingTax.Entities;

namespace E_POS.Application.Modules.PricingTax.Services;

public sealed class PriceListItemsService : IPriceListItemsService
{
    private static readonly ApplicationError PermissionDenied = new("pricing.price_list_item.permission_denied", "Permission denied for price list items management.");
    private static readonly ApplicationError NotFound = new("pricing.price_list_item.not_found", "Price list item was not found.");

    private readonly IPriceListItemsRepository _repository;
    private readonly IPriceListRepository _priceListRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfMeasureRepository _uomRepository;
    private readonly IPriceListItemsRequestValidator _validator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PriceListItemsService(
        IPriceListItemsRepository repository,
        IPriceListRepository priceListRepository,
        IProductRepository productRepository,
        IUnitOfMeasureRepository uomRepository,
        IPriceListItemsRequestValidator validator,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _priceListRepository = priceListRepository;
        _productRepository = productRepository;
        _uomRepository = uomRepository;
        _validator = validator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<PriceListItemResponse>> CreateAsync(
        TenantRequestContext context,
        PriceListItemCreateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxConstants.UpdatePermission);
        if (accessError is not null) return ApplicationResult<PriceListItemResponse>.Failure(accessError);

        var validationError = _validator.ValidateCreate(request);
        if (validationError is not null) return ApplicationResult<PriceListItemResponse>.Failure(validationError);

        var priceList = await _priceListRepository.GetEditableAsync(context.TenantId, request.PriceListId, cancellationToken);
        if (priceList is null)
        {
            return ApplicationResult<PriceListItemResponse>.Failure(new ApplicationError("pricing.price_list_item.invalid_price_list", "Price list does not exist."));
        }

        if (!await _productRepository.ProductExistsAsync(context.TenantId, request.ProductId, cancellationToken))
        {
            return ApplicationResult<PriceListItemResponse>.Failure(new ApplicationError("pricing.price_list_item.invalid_product", "Product does not exist."));
        }

        if (request.ProductVariantId.HasValue && request.ProductVariantId.Value != Guid.Empty)
        {
            if (!await _productRepository.ProductVariantExistsAsync(context.TenantId, request.ProductId, request.ProductVariantId.Value, cancellationToken))
            {
                return ApplicationResult<PriceListItemResponse>.Failure(new ApplicationError("pricing.price_list_item.invalid_variant", "Product variant does not exist."));
            }
        }

        if (request.UomId.HasValue && request.UomId.Value != Guid.Empty)
        {
            if (!await _uomRepository.UomExistsAsync(context.TenantId, request.UomId.Value, cancellationToken))
            {
                return ApplicationResult<PriceListItemResponse>.Failure(new ApplicationError("pricing.price_list_item.invalid_uom", "Unit of measure does not exist."));
            }
        }

        var hasDuplicate = await _repository.ItemExistsAsync(
            context.TenantId,
            request.PriceListId,
            request.ProductId,
            request.ProductVariantId,
            request.UomId,
            request.MinQuantity,
            null,
            cancellationToken);

        if (hasDuplicate)
        {
            return ApplicationResult<PriceListItemResponse>.Failure(new ApplicationError("pricing.price_list_item.duplicate_entry", "A pricing configuration already exists for this combination and minimum quantity."));
        }

        var itemId = Guid.NewGuid();
        var now = _dateTimeProvider.UtcNow;

        var priceListItem = PriceListItem.Create(
            id: itemId,
            tenantId: context.TenantId,
            priceListId: request.PriceListId,
            productId: request.ProductId,
            productVariantId: request.ProductVariantId,
            uomId: request.UomId,
            sellingPrice: request.SellingPrice,
            compareAtPrice: request.CompareAtPrice,
            minQuantity: request.MinQuantity,
            validFrom: request.ValidFrom,
            validUntil: request.ValidUntil,
            status: request.Status,
            createdByTenantUserId: context.UserId,
            now: now);

        await _repository.AddAsync(priceListItem, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var response = await _repository.GetByIdAsync(context.TenantId, itemId, cancellationToken);
        return ApplicationResult<PriceListItemResponse>.Success(response!);
    }

    public async Task<ApplicationResult<PriceListItemListResponse>> ListAsync(
        TenantRequestContext context,
        Guid priceListId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<PriceListItemListResponse>.Failure(accessError);

        var priceList = await _priceListRepository.GetByIdAsync(context.TenantId, priceListId, cancellationToken);
        if (priceList is null)
        {
            return ApplicationResult<PriceListItemListResponse>.Failure(new ApplicationError("pricing.price_list_item.invalid_price_list", "Price list does not exist."));
        }

        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var response = await _repository.ListAsync(context.TenantId, priceListId, safePageNumber, safePageSize, cancellationToken);
        return ApplicationResult<PriceListItemListResponse>.Success(response);
    }

    public async Task<ApplicationResult<PriceListItemResponse>> GetByIdAsync(
        TenantRequestContext context,
        Guid id,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<PriceListItemResponse>.Failure(accessError);

        var response = await _repository.GetByIdAsync(context.TenantId, id, cancellationToken);
        if (response is null) return ApplicationResult<PriceListItemResponse>.Failure(NotFound);

        return ApplicationResult<PriceListItemResponse>.Success(response);
    }

    public async Task<ApplicationResult<PriceListItemResponse>> UpdateAsync(
        TenantRequestContext context,
        Guid id,
        PriceListItemUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxConstants.UpdatePermission);
        if (accessError is not null) return ApplicationResult<PriceListItemResponse>.Failure(accessError);

        var validationError = _validator.ValidateUpdate(request);
        if (validationError is not null) return ApplicationResult<PriceListItemResponse>.Failure(validationError);

        var item = await _repository.GetEditableAsync(context.TenantId, id, cancellationToken);
        if (item is null) return ApplicationResult<PriceListItemResponse>.Failure(NotFound);

        var hasDuplicate = await _repository.ItemExistsAsync(
            context.TenantId,
            item.PriceListId,
            item.ProductId,
            item.ProductVariantId,
            item.UomId,
            request.MinQuantity,
            id,
            cancellationToken);

        if (hasDuplicate)
        {
            return ApplicationResult<PriceListItemResponse>.Failure(new ApplicationError("pricing.price_list_item.duplicate_entry", "A pricing configuration already exists for this combination and minimum quantity."));
        }

        var now = _dateTimeProvider.UtcNow;
        item.UpdateProfile(
            sellingPrice: request.SellingPrice,
            compareAtPrice: request.CompareAtPrice,
            minQuantity: request.MinQuantity,
            validFrom: request.ValidFrom,
            validUntil: request.ValidUntil,
            status: request.Status,
            updatedByTenantUserId: context.UserId,
            now: now);

        await _repository.SaveChangesAsync(cancellationToken);

        var response = await _repository.GetByIdAsync(context.TenantId, id, cancellationToken);
        return ApplicationResult<PriceListItemResponse>.Success(response!);
    }

    public async Task<ApplicationResult<Guid>> DeleteAsync(
        TenantRequestContext context,
        Guid id,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PricingTaxConstants.DeletePermission);
        if (accessError is not null) return ApplicationResult<Guid>.Failure(accessError);

        var item = await _repository.GetEditableAsync(context.TenantId, id, cancellationToken);
        if (item is null) return ApplicationResult<Guid>.Failure(NotFound);

        var now = _dateTimeProvider.UtcNow;
        item.SoftDelete(context.UserId, now);

        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(id);
    }

    private static ApplicationError? ValidateAccess(TenantRequestContext context, string requiredPermission)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("pricing.price_list_item.invalid_tenant_context", "Invalid tenant context.");
        }
        if (!context.HasPermission(requiredPermission) && !context.HasPermission(PricingTaxConstants.ManagePermission))
        {
            return PermissionDenied;
        }
        return null;
    }
}
