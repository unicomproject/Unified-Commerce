using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class ProductService : IProductService
{
    private static readonly ApplicationError PermissionDenied = new("product.permission_denied", "Permission denied for product management.");
    private static readonly ApplicationError NotFound = new("product.not_found", "Product was not found.");
    private readonly IProductRepository _repository;
    private readonly IProductRequestValidator _validator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ProductService(IProductRepository repository, IProductRequestValidator validator, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _validator = validator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<ProductResponse>> CreateAsync(TenantRequestContext context, ProductCreateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, ProductConstants.CreatePermission);
        if (accessError is not null) return ApplicationResult<ProductResponse>.Failure(accessError);

        var validationError = _validator.ValidateCreate(request);
        if (validationError is not null) return ApplicationResult<ProductResponse>.Failure(validationError);

        var normalizedCode = ProductConstants.NormalizeCode(request.ProductCode);
        if (await _repository.ProductCodeExistsAsync(context.TenantId, normalizedCode, null, cancellationToken))
        {
            return ApplicationResult<ProductResponse>.Failure(new ApplicationError("product.duplicate_code", "Product code already exists."));
        }

        if (await _repository.SkuExistsAsync(context.TenantId, request.Sku, null, cancellationToken))
        {
            return ApplicationResult<ProductResponse>.Failure(new ApplicationError("product.duplicate_sku", "SKU already exists."));
        }

        if (request.Barcode != null && await _repository.BarcodeExistsAsync(context.TenantId, request.Barcode, null, cancellationToken))
        {
            return ApplicationResult<ProductResponse>.Failure(new ApplicationError("product.duplicate_barcode", "Barcode already exists."));
        }

        var productId = Guid.NewGuid();
        var now = _dateTimeProvider.UtcNow;

        // Generate a URL-safe slug from name + code for uniqueness
        var slug = GenerateSlug(request.Name, normalizedCode);

        var product = Product.Create(
            id: productId,
            tenantId: context.TenantId,
            productCode: normalizedCode,
            productName: request.Name,
            productSlug: slug,
            productType: request.ProductType ?? "STANDARD",
            productStructure: request.ProductStructure ?? "SIMPLE",
            businessTypeId: request.BusinessTypeId,
            brandId: request.BrandId,
            returnPolicyId: request.ReturnPolicyId,
            shortDescription: request.ShortDescription ?? request.Description,
            longDescription: request.LongDescription,
            isSellable: request.IsSellable ?? true,
            isTaxable: request.IsTaxable ?? true,
            status: request.Status,
            createdByTenantUserId: context.UserId,
            now: now);
        await _repository.AddAsync(product, cancellationToken);

        var variantId = Guid.NewGuid();
        var defaultVariant = ProductVariant.Create(
            id: variantId,
            tenantId: context.TenantId,
            productId: productId,
            variantCode: "DEFAULT",
            variantName: request.Name,
            sku: request.Sku,
            stockUomId: request.StockUomId ?? Guid.Empty,
            salesUomId: request.SalesUomId ?? Guid.Empty,
            isDefaultVariant: true,
            isSellable: true,
            allowFractionalQuantity: false,
            status: request.Status,
            createdByTenantUserId: context.UserId,
            now: now);
        await _repository.AddVariantAsync(defaultVariant, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            var barcode = ProductBarcode.Create(
                id: Guid.NewGuid(),
                tenantId: context.TenantId,
                productId: productId,
                productVariantId: variantId,
                barcode: request.Barcode,
                barcodeType: "EAN13",
                uomId: null,
                quantityPerScan: 1m,
                isPrimaryBarcode: true,
                status: "ACTIVE",
                createdByTenantUserId: context.UserId,
                now: now);
            await _repository.AddBarcodeAsync(barcode, cancellationToken);
        }

        if (request.CategoryIds != null && request.CategoryIds.Length > 0)
        {
            var categoryLinks = request.CategoryIds.Select((catId, idx) =>
                ProductCategory.Create(Guid.NewGuid(), context.TenantId, productId, catId, idx == 0, idx, context.UserId, now));
            await _repository.AddCategoryLinksAsync(categoryLinks, cancellationToken);
        }

        if (request.CollectionIds != null && request.CollectionIds.Length > 0)
        {
            var collectionLinks = request.CollectionIds.Select((collId, idx) =>
                ProductCollection.Create(Guid.NewGuid(), context.TenantId, productId, collId, idx, context.UserId, now));
            await _repository.AddCollectionLinksAsync(collectionLinks, cancellationToken);
        }

        var legacyImages = BuildProductImagesFromLegacyUrls(
            context.TenantId,
            productId,
            request.ImageUrls,
            context.UserId,
            now);

        if (legacyImages.MediaAssets.Count > 0)
        {
            await _repository.AddMediaAssetsAsync(legacyImages.MediaAssets, cancellationToken);
        }

        if (legacyImages.ProductImages.Count > 0)
        {
            await _repository.AddImagesAsync(legacyImages.ProductImages, cancellationToken);
        }

        if (request.SalesChannelIds != null && request.SalesChannelIds.Length > 0)
        {
            var channelVisibilities = request.SalesChannelIds.Select(channelId =>
                ProductChannelVisibility.Create(
                    id: Guid.NewGuid(),
                    tenantId: context.TenantId,
                    productId: productId,
                    productVariantId: null,
                    salesChannelId: channelId,
                    isVisible: true,
                    isOrderable: true,
                    availableFrom: null,
                    availableUntil: null,
                    status: "ACTIVE",
                    createdByTenantUserId: context.UserId,
                    now: now));
            await _repository.AddChannelVisibilitiesAsync(channelVisibilities, cancellationToken);
        }

        if (request.Price.HasValue)
        {
            var defaultPriceListId = await _repository.GetDefaultPriceListIdAsync(context.TenantId, cancellationToken);
            if (defaultPriceListId.HasValue)
            {
                var priceListItem = new PriceListItem();
                SetPropertyValue(priceListItem, "Id", Guid.NewGuid());
                SetPropertyValue(priceListItem, "TenantId", context.TenantId);
                SetPropertyValue(priceListItem, "PriceListId", defaultPriceListId.Value);
                SetPropertyValue(priceListItem, "ProductId", productId);
                SetPropertyValue(priceListItem, "ProductVariantId", variantId);
                SetPropertyValue(priceListItem, "SellingPrice", request.Price.Value);
                SetPropertyValue(priceListItem, "MinQuantity", 1m);
                SetPropertyValue(priceListItem, "Status", "ACTIVE");
                SetPropertyValue(priceListItem, "CreatedAt", now);
                SetPropertyValue(priceListItem, "UpdatedAt", now);

                await _repository.AddPriceListItemAsync(priceListItem, cancellationToken);
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);

        var response = await _repository.GetByIdAsync(context.TenantId, productId, false, cancellationToken);
        return ApplicationResult<ProductResponse>.Success(response!);
    }

    public async Task<ApplicationResult<ProductListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, ProductConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<ProductListResponse>.Failure(accessError);

        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var response = await _repository.ListAsync(context.TenantId, safePageNumber, safePageSize, search, cancellationToken);
        return ApplicationResult<ProductListResponse>.Success(response);
    }

    public async Task<ApplicationResult<ProductResponse>> GetByIdAsync(TenantRequestContext context, Guid productId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, ProductConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<ProductResponse>.Failure(accessError);

        var response = await _repository.GetByIdAsync(context.TenantId, productId, false, cancellationToken);
        if (response is null) return ApplicationResult<ProductResponse>.Failure(NotFound);

        return ApplicationResult<ProductResponse>.Success(response);
    }

    public async Task<ApplicationResult<ProductResponse>> UpdateAsync(TenantRequestContext context, Guid productId, ProductUpdateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, ProductConstants.UpdatePermission);
        if (accessError is not null) return ApplicationResult<ProductResponse>.Failure(accessError);

        var validationError = _validator.ValidateUpdate(request);
        if (validationError is not null) return ApplicationResult<ProductResponse>.Failure(validationError);

        var product = await _repository.GetEditableAsync(context.TenantId, productId, cancellationToken);
        if (product is null) return ApplicationResult<ProductResponse>.Failure(NotFound);

        var normalizedCode = ProductConstants.NormalizeCode(request.ProductCode);
        if (await _repository.ProductCodeExistsAsync(context.TenantId, normalizedCode, productId, cancellationToken))
        {
            return ApplicationResult<ProductResponse>.Failure(new ApplicationError("product.duplicate_code", "Product code already exists."));
        }

        var defaultVariant = await _repository.GetDefaultVariantAsync(productId, cancellationToken);
        if (defaultVariant != null)
        {
            if (await _repository.SkuExistsAsync(context.TenantId, request.Sku, defaultVariant.Id, cancellationToken))
            {
                return ApplicationResult<ProductResponse>.Failure(new ApplicationError("product.duplicate_sku", "SKU already exists."));
            }

            if (request.Barcode != null && await _repository.BarcodeExistsAsync(context.TenantId, request.Barcode, defaultVariant.Id, cancellationToken))
            {
                return ApplicationResult<ProductResponse>.Failure(new ApplicationError("product.duplicate_barcode", "Barcode already exists."));
            }
        }

        var now = _dateTimeProvider.UtcNow;
        var slug = GenerateSlug(request.Name, normalizedCode);

        product.UpdateProfile(
            productCode: normalizedCode,
            productName: request.Name,
            productSlug: slug,
            productType: request.ProductType ?? "STANDARD",
            productStructure: request.ProductStructure ?? "SIMPLE",
            businessTypeId: request.BusinessTypeId,
            brandId: request.BrandId,
            returnPolicyId: request.ReturnPolicyId,
            shortDescription: request.ShortDescription ?? request.Description,
            longDescription: request.LongDescription,
            isSellable: request.IsSellable ?? true,
            isTaxable: request.IsTaxable ?? true,
            status: request.Status,
            updatedByTenantUserId: context.UserId,
            now: now);

        if (defaultVariant != null)
        {
            defaultVariant.UpdateProfile(
                variantCode: "DEFAULT",
                variantName: request.Name,
                sku: request.Sku,
                stockUomId: request.StockUomId ?? defaultVariant.StockUomId,
                salesUomId: request.SalesUomId ?? defaultVariant.SalesUomId,
                isDefaultVariant: true,
                isSellable: true,
                allowFractionalQuantity: false,
                status: request.Status,
                updatedByTenantUserId: context.UserId,
                now: now);
        }

        var replaceLegacyImages = request.ImageUrls is not null;
        var replacedMediaAssetIds = replaceLegacyImages
            ? await _repository.GetProductImageMediaAssetIdsAsync(context.TenantId, productId, cancellationToken)
            : Array.Empty<Guid>();

        await _repository.ClearProductMappingsAsync(context.TenantId, productId, replaceLegacyImages, cancellationToken);

        if (replacedMediaAssetIds.Count > 0)
        {
            await _repository.MarkMediaAssetsInactiveAsync(
                context.TenantId,
                replacedMediaAssetIds,
                context.UserId,
                now,
                cancellationToken);
        }

        if (defaultVariant != null && !string.IsNullOrWhiteSpace(request.Barcode))
        {
            var barcode = ProductBarcode.Create(
                id: Guid.NewGuid(),
                tenantId: context.TenantId,
                productId: productId,
                productVariantId: defaultVariant.Id,
                barcode: request.Barcode,
                barcodeType: "EAN13",
                uomId: null,
                quantityPerScan: 1m,
                isPrimaryBarcode: true,
                status: "ACTIVE",
                createdByTenantUserId: context.UserId,
                now: now);
            await _repository.AddBarcodeAsync(barcode, cancellationToken);
        }

        if (request.CategoryIds != null && request.CategoryIds.Length > 0)
        {
            var categoryLinks = request.CategoryIds.Select((catId, idx) =>
                ProductCategory.Create(Guid.NewGuid(), context.TenantId, productId, catId, idx == 0, idx, context.UserId, now));
            await _repository.AddCategoryLinksAsync(categoryLinks, cancellationToken);
        }

        if (request.CollectionIds != null && request.CollectionIds.Length > 0)
        {
            var collectionLinks = request.CollectionIds.Select((collId, idx) =>
                ProductCollection.Create(Guid.NewGuid(), context.TenantId, productId, collId, idx, context.UserId, now));
            await _repository.AddCollectionLinksAsync(collectionLinks, cancellationToken);
        }

        var legacyImages = BuildProductImagesFromLegacyUrls(
            context.TenantId,
            productId,
            request.ImageUrls,
            context.UserId,
            now);

        if (legacyImages.MediaAssets.Count > 0)
        {
            await _repository.AddMediaAssetsAsync(legacyImages.MediaAssets, cancellationToken);
        }

        if (legacyImages.ProductImages.Count > 0)
        {
            await _repository.AddImagesAsync(legacyImages.ProductImages, cancellationToken);
        }

        if (request.SalesChannelIds != null && request.SalesChannelIds.Length > 0)
        {
            var channelVisibilities = request.SalesChannelIds.Select(channelId =>
                ProductChannelVisibility.Create(
                    id: Guid.NewGuid(),
                    tenantId: context.TenantId,
                    productId: productId,
                    productVariantId: null,
                    salesChannelId: channelId,
                    isVisible: true,
                    isOrderable: true,
                    availableFrom: null,
                    availableUntil: null,
                    status: "ACTIVE",
                    createdByTenantUserId: context.UserId,
                    now: now));
            await _repository.AddChannelVisibilitiesAsync(channelVisibilities, cancellationToken);
        }

        if (request.Price.HasValue && defaultVariant != null)
        {
            var defaultPriceListId = await _repository.GetDefaultPriceListIdAsync(context.TenantId, cancellationToken);
            if (defaultPriceListId.HasValue)
            {
                var existingPrice = await _repository.GetPriceListItemAsync(defaultPriceListId.Value, defaultVariant.Id, cancellationToken);
                if (existingPrice != null)
                {
                    SetPropertyValue(existingPrice, "SellingPrice", request.Price.Value);
                    SetPropertyValue(existingPrice, "UpdatedAt", now);
                }
                else
                {
                    var priceListItem = new PriceListItem();
                    SetPropertyValue(priceListItem, "Id", Guid.NewGuid());
                    SetPropertyValue(priceListItem, "TenantId", context.TenantId);
                    SetPropertyValue(priceListItem, "PriceListId", defaultPriceListId.Value);
                    SetPropertyValue(priceListItem, "ProductId", productId);
                    SetPropertyValue(priceListItem, "ProductVariantId", defaultVariant.Id);
                    SetPropertyValue(priceListItem, "SellingPrice", request.Price.Value);
                    SetPropertyValue(priceListItem, "MinQuantity", 1m);
                    SetPropertyValue(priceListItem, "Status", "ACTIVE");
                    SetPropertyValue(priceListItem, "CreatedAt", now);
                    SetPropertyValue(priceListItem, "UpdatedAt", now);

                    await _repository.AddPriceListItemAsync(priceListItem, cancellationToken);
                }
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);

        var response = await _repository.GetByIdAsync(context.TenantId, productId, false, cancellationToken);
        return ApplicationResult<ProductResponse>.Success(response!);
    }

    public async Task<ApplicationResult<Guid>> DeleteAsync(TenantRequestContext context, Guid productId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, ProductConstants.DeletePermission);
        if (accessError is not null) return ApplicationResult<Guid>.Failure(accessError);

        var product = await _repository.GetEditableAsync(context.TenantId, productId, cancellationToken);
        if (product is null) return ApplicationResult<Guid>.Failure(NotFound);

        var now = _dateTimeProvider.UtcNow;
        var mediaAssetIds = await _repository.GetProductImageMediaAssetIdsAsync(
            context.TenantId,
            productId,
            cancellationToken);

        product.SoftDelete(context.UserId, now);

        var defaultVariant = await _repository.GetDefaultVariantAsync(productId, cancellationToken);
        if (defaultVariant != null)
        {
            defaultVariant.SoftDelete(context.UserId, now);
        }

        if (mediaAssetIds.Count > 0)
        {
            await _repository.MarkMediaAssetsInactiveAsync(
                context.TenantId,
                mediaAssetIds,
                context.UserId,
                now,
                cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(productId);
    }

    private static ProductLegacyImageBuildResult BuildProductImagesFromLegacyUrls(
        Guid tenantId,
        Guid productId,
        IReadOnlyCollection<string>? imageUrls,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        if (imageUrls is null || imageUrls.Count == 0)
        {
            return new ProductLegacyImageBuildResult([], []);
        }

        var mediaAssets = new List<MediaAsset>();
        var productImages = new List<ProductImage>();
        var sortOrder = 0;

        foreach (var imageUrl in imageUrls)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                continue;
            }

            var normalizedImageUrl = imageUrl.Trim();
            var mediaAsset = LegacyMediaAssetFactory.CreateImageFromUrl(
                tenantId,
                productId,
                "products",
                "PRODUCT",
                normalizedImageUrl,
                createdByTenantUserId,
                now);

            if (mediaAsset is not null)
            {
                mediaAssets.Add(mediaAsset);
            }

            productImages.Add(ProductImage.Create(
                id: Guid.NewGuid(),
                tenantId: tenantId,
                productId: productId,
                productVariantId: null,
                salesChannelId: null,
                imageStorageKey: normalizedImageUrl,
                imageUrl: normalizedImageUrl,
                altText: null,
                imagePurpose: "PRODUCT",
                mimeType: mediaAsset?.MimeType,
                fileSizeBytes: mediaAsset?.FileSizeBytes,
                widthPx: null,
                heightPx: null,
                checksumHash: mediaAsset?.ChecksumHash,
                sortOrder: sortOrder,
                isPrimaryImage: sortOrder == 0,
                status: "ACTIVE",
                createdByTenantUserId: createdByTenantUserId,
                now: now,
                mediaAssetId: mediaAsset?.Id));

            sortOrder++;
        }

        return new ProductLegacyImageBuildResult(productImages, mediaAssets);
    }
    private static string GenerateSlug(string name, string code)
    {
        var slugBase = name.Trim().ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");
        return $"{slugBase}-{code.ToLowerInvariant()}";
    }

    private static ApplicationError? ValidateAccess(TenantRequestContext context, string requiredPermission)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("product.invalid_tenant_context", "Invalid tenant context.");
        }
        if (!context.HasPermission(requiredPermission) && !context.HasPermission(ProductConstants.ManagePermission))
        {
            return PermissionDenied;
        }
        return null;
    }

    private static void SetPropertyValue<T>(object target, string propertyName, T value)
    {
        var prop = target.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        prop?.SetValue(target, value);
    }
    private sealed record ProductLegacyImageBuildResult(
        IReadOnlyList<ProductImage> ProductImages,
        IReadOnlyList<MediaAsset> MediaAssets);
}


