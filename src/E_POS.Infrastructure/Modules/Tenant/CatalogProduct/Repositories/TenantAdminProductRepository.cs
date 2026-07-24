using E_POS.Application.Modules.Shared.Media;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed partial class TenantAdminProductRepository : ITenantAdminProductRepository
{
    private readonly EPosDbContext _dbContext;

    public TenantAdminProductRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantAdminProductSummaryResponse> GetSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var products = _dbContext.Products
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != ProductConstants.DeletedStatus);

        var totalProducts = await products.CountAsync(cancellationToken);
        var activeProducts = await products.CountAsync(
            x => x.Status == ProductConstants.ActiveStatus,
            cancellationToken);
        var inactiveProducts = await products.CountAsync(
            x => x.Status == ProductConstants.InactiveStatus,
            cancellationToken);

        var productCategories = await (
            from link in _dbContext.ProductCategories.AsNoTracking()
            join product in products on link.ProductId equals product.Id
            select link.CategoryId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new TenantAdminProductSummaryResponse(
            totalProducts,
            activeProducts,
            inactiveProducts,
            productCategories);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetPrimaryCategoryNamesAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var rows = await (
            from link in _dbContext.ProductCategories.AsNoTracking()
            join category in _dbContext.Categories.AsNoTracking()
                on link.CategoryId equals category.Id
            where link.TenantId == tenantId && productIds.Contains(link.ProductId)
            orderby link.IsPrimaryCategory descending, link.SortOrder
            select new
            {
                link.ProductId,
                category.CategoryName,
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.First().CategoryName);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetPrimaryImageUrlsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var rows = await (
            from image in _dbContext.ProductImages.AsNoTracking()
            join mediaAsset in _dbContext.Set<MediaAsset>().AsNoTracking()
                on new { image.TenantId, MediaAssetId = image.MediaAssetId }
                equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
            from mediaAsset in mediaAssets.DefaultIfEmpty()
            where image.TenantId == tenantId &&
                  productIds.Contains(image.ProductId) &&
                  image.Status == "ACTIVE"
            select new
            {
                image.Id,
                image.ProductId,
                image.ProductVariantId,
                image.MediaAssetId,
                image.ImageUrl,
                image.ImageStorageKey,
                image.SortOrder,
                image.IsPrimaryImage,
                JoinedMediaAssetId = mediaAsset == null ? null : (Guid?)mediaAsset.Id,
                MediaStatus = mediaAsset == null ? null : mediaAsset.Status,
                MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Where(row =>
                !row.MediaAssetId.HasValue ||
                (row.JoinedMediaAssetId.HasValue && row.MediaStatus == "ACTIVE"))
            .Select(row => new
            {
                row.Id,
                row.ProductId,
                row.ProductVariantId,
                row.SortOrder,
                row.IsPrimaryImage,
                ImageUrl = MediaUrlResolver.PreferMediaAsset(
                    row.MediaPublicUrl,
                    row.ImageUrl,
                    row.ImageStorageKey),
            })
            .Where(row => !string.IsNullOrWhiteSpace(row.ImageUrl))
            .GroupBy(row => row.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(row => row.ProductVariantId.HasValue ? 1 : 0)
                    .ThenByDescending(row => row.IsPrimaryImage)
                    .ThenBy(row => row.SortOrder)
                    .ThenBy(row => row.Id)
                    .First()
                    .ImageUrl!);
    }

    public async Task<TenantAdminProductCreateOptionsResponse> GetCreateOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Status == CategoryConstants.ActiveStatus &&
                x.ParentCategoryId == null)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CategoryName)
            .Select(x => new TenantAdminProductCategoryOptionResponse(
                x.Id,
                x.CategoryName,
                x.CategoryCode))
            .ToListAsync(cancellationToken);

        var subCategories = await _dbContext.Categories
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Status == CategoryConstants.ActiveStatus &&
                x.ParentCategoryId != null)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CategoryName)
            .Select(x => new TenantAdminProductSubCategoryOptionResponse(
                x.Id,
                x.CategoryName,
                x.CategoryCode,
                x.ParentCategoryId!.Value))
            .ToListAsync(cancellationToken);

        var brands = await _dbContext.Brands
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Status == BrandConstants.ActiveStatus)
            .OrderBy(x => x.BrandName)
            .Select(x => new TenantAdminProductBrandOptionResponse(
                x.Id,
                x.BrandName,
                x.BrandCode))
            .ToListAsync(cancellationToken);

        var units = await _dbContext.UnitOfMeasures
            .AsNoTracking()
            .Where(x =>
                (x.TenantId == null || x.TenantId == tenantId) &&
                x.Status != "DELETED")
            .OrderBy(x => x.TenantId == null ? 0 : 1)
            .ThenBy(x => x.UomCode)
            .Select(x => new TenantAdminProductUnitOptionResponse(
                x.Id,
                x.UomCode,
                x.UomName))
            .ToListAsync(cancellationToken);

        var taxes = await _dbContext.TaxClasses
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == "ACTIVE")
            .OrderByDescending(x => x.IsDefaultTaxClass)
            .ThenBy(x => x.TaxClassName)
            .Select(x => new TenantAdminProductTaxOptionResponse(
                x.Id,
                x.TaxClassCode,
                x.TaxClassName))
            .ToListAsync(cancellationToken);

        var outlets = await _dbContext.Outlets
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Status == OutletConstants.ActiveStatus)
            .OrderBy(x => x.OutletName)
            .Select(x => new TenantAdminProductOutletOptionResponse(
                x.Id,
                x.OutletName,
                x.OutletCode))
            .ToListAsync(cancellationToken);

        var variantOptionTemplates = await _dbContext.ProductOptionTemplates
            .AsNoTracking()
            .Where(x => x.Status == "ACTIVE")
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.TemplateName)
            .Select(x => new TenantAdminProductVariantOptionTemplateResponse(
                x.Id,
                x.TemplateCode,
                x.TemplateName,
                x.OptionType))
            .ToListAsync(cancellationToken);

        return new TenantAdminProductCreateOptionsResponse(
            categories,
            subCategories,
            brands,
            units,
            taxes,
            outlets,
            variantOptionTemplates);
    }

    public async Task<Guid?> ResolveUnitIdAsync(
        Guid tenantId,
        string unitType,
        CancellationToken cancellationToken)
    {
        if (Guid.TryParse(unitType, out var unitId))
        {
            var existsById = await _dbContext.UnitOfMeasures
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == unitId &&
                         (x.TenantId == null || x.TenantId == tenantId) &&
                         x.Status != "DELETED",
                    cancellationToken);

            return existsById ? unitId : null;
        }

        var normalizedCode = unitType.Trim().ToUpperInvariant();
        return await _dbContext.UnitOfMeasures
            .AsNoTracking()
            .Where(x =>
                (x.TenantId == null || x.TenantId == tenantId) &&
                x.Status != "DELETED" &&
                x.UomCode.ToUpper() == normalizedCode)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> CategoryBelongsToTenantAsync(
        Guid tenantId,
        Guid categoryId,
        Guid? parentCategoryId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Id == categoryId &&
                     x.Status == CategoryConstants.ActiveStatus &&
                     x.ParentCategoryId == parentCategoryId,
                cancellationToken);
    }

    public Task<bool> BrandBelongsToTenantAsync(
        Guid tenantId,
        Guid brandId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Brands
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Id == brandId &&
                     x.Status == BrandConstants.ActiveStatus,
                cancellationToken);
    }

    public Task<bool> TaxClassBelongsToTenantAsync(
        Guid tenantId,
        Guid taxClassId,
        CancellationToken cancellationToken)
    {
        return _dbContext.TaxClasses
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Id == taxClassId &&
                     x.Status == "ACTIVE",
                cancellationToken);
    }

    public async Task<bool> OutletsBelongToTenantAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> outletIds,
        CancellationToken cancellationToken)
    {
        if (outletIds.Count == 0)
        {
            return true;
        }

        var distinctIds = outletIds.Distinct().ToArray();
        var count = await _dbContext.Outlets
            .AsNoTracking()
            .CountAsync(
                x => x.TenantId == tenantId &&
                     x.Status == OutletConstants.ActiveStatus &&
                     distinctIds.Contains(x.Id),
                cancellationToken);

        return count == distinctIds.Length;
    }

    public async Task<TenantAdminProductDetailResponse?> GetDetailAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == productId &&
                     x.Status != ProductConstants.DeletedStatus,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        var variants = await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status != ProductConstants.DeletedStatus)
            .OrderByDescending(x => x.IsDefaultVariant)
            .ThenBy(x => x.VariantCode)
            .ToListAsync(cancellationToken);

        var defaultVariant = variants.FirstOrDefault();
        var variantIds = variants.Select(x => x.Id).ToArray();

        var defaultPriceListId = await GetDefaultPriceListIdAsync(tenantId, cancellationToken);

        var barcodesByVariant = variantIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.ProductBarcodes
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.ProductVariantId != null &&
                    variantIds.Contains(x.ProductVariantId.Value) &&
                    x.Status != "DELETED")
                .GroupBy(x => x.ProductVariantId!.Value)
                .Select(group => new
                {
                    VariantId = group.Key,
                    Barcode = group.OrderByDescending(x => x.IsPrimaryBarcode).First().Barcode,
                })
                .ToDictionaryAsync(x => x.VariantId, x => x.Barcode, cancellationToken);

        var pricesByVariant = new Dictionary<Guid, (decimal SellingPrice, decimal? CompareAtPrice)>();
        if (defaultPriceListId.HasValue && variantIds.Length > 0)
        {
            var priceRows = await _dbContext.PriceListItems
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.PriceListId == defaultPriceListId.Value &&
                    x.ProductId == productId &&
                    x.ProductVariantId != null &&
                    variantIds.Contains(x.ProductVariantId.Value) &&
                    x.Status != "DELETED")
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            foreach (var group in priceRows.GroupBy(x => x.ProductVariantId!.Value))
            {
                var latest = group.First();
                pricesByVariant[group.Key] = (latest.SellingPrice, latest.CompareAtPrice);
            }
        }

        var categoryRows = await (
            from link in _dbContext.ProductCategories.AsNoTracking()
            join category in _dbContext.Categories.AsNoTracking()
                on link.CategoryId equals category.Id
            where link.TenantId == tenantId && link.ProductId == productId
            orderby link.IsPrimaryCategory descending, link.SortOrder
            select new
            {
                category.Id,
                category.CategoryName,
                category.ParentCategoryId,
            })
            .ToListAsync(cancellationToken);

        var parentCategory = categoryRows.FirstOrDefault(x => x.ParentCategoryId == null);
        var subCategory = categoryRows.FirstOrDefault(x => x.ParentCategoryId != null);
        var categoryId = parentCategory?.Id ?? subCategory?.Id ?? Guid.Empty;
        var categoryName = parentCategory?.CategoryName ?? subCategory?.CategoryName ?? string.Empty;

        var imageRows = await (
            from image in _dbContext.ProductImages.AsNoTracking()
            join mediaAsset in _dbContext.Set<MediaAsset>().AsNoTracking()
                on new { image.TenantId, MediaAssetId = image.MediaAssetId }
                equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
            from mediaAsset in mediaAssets.DefaultIfEmpty()
            where image.TenantId == tenantId &&
                  image.ProductId == productId &&
                  image.Status == "ACTIVE"
            select new
            {
                image.Id,
                image.ProductVariantId,
                image.MediaAssetId,
                image.ImageUrl,
                image.ImageStorageKey,
                image.AltText,
                image.ImagePurpose,
                image.SortOrder,
                image.IsPrimaryImage,
                JoinedMediaAssetId = mediaAsset == null ? null : (Guid?)mediaAsset.Id,
                MediaStatus = mediaAsset == null ? null : mediaAsset.Status,
                MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl,
            })
            .ToListAsync(cancellationToken);

        var images = imageRows
            .Where(row =>
                !row.MediaAssetId.HasValue ||
                (row.JoinedMediaAssetId.HasValue && row.MediaStatus == "ACTIVE"))
            .Select(row => new
            {
                Row = row,
                ImageUrl = MediaUrlResolver.PreferMediaAsset(
                    row.MediaPublicUrl,
                    row.ImageUrl,
                    row.ImageStorageKey),
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.ImageUrl))
            .OrderBy(item => item.Row.ProductVariantId.HasValue ? 1 : 0)
            .ThenByDescending(item => item.Row.IsPrimaryImage)
            .ThenBy(item => item.Row.SortOrder)
            .ThenBy(item => item.Row.Id)
            .Select(item => new TenantAdminProductImageResponse(
                item.Row.Id,
                item.Row.MediaAssetId,
                item.Row.ProductVariantId,
                item.ImageUrl!,
                item.Row.AltText,
                item.Row.ImagePurpose,
                item.Row.SortOrder,
                item.Row.IsPrimaryImage))
            .ToList();

        var productImages = images
            .Where(image => image.ProductVariantId is null)
            .ToList();
        var imageUrl = images.FirstOrDefault()?.ImageUrl;
        var taxInfo = await (
            from assignment in _dbContext.ProductTaxAssignments.AsNoTracking()
            join tax in _dbContext.TaxClasses.AsNoTracking()
                on assignment.TaxClassId equals tax.Id
            where assignment.TenantId == tenantId &&
                  assignment.ProductId == productId &&
                  assignment.Status != "DELETED"
            orderby assignment.ProductVariantId == null descending, assignment.CreatedAt descending
            select new
            {
                tax.Id,
                tax.TaxClassName,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var unitType = string.Empty;
        if (defaultVariant is not null)
        {
            unitType = await _dbContext.UnitOfMeasures
                .AsNoTracking()
                .Where(x => x.Id == defaultVariant.StockUomId)
                .Select(x => x.UomCode)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        }

        var balanceRows = await (
            from balance in _dbContext.InventoryBalances.AsNoTracking()
            join location in _dbContext.InventoryLocations.AsNoTracking()
                on balance.InventoryLocationId equals location.Id
            join outlet in _dbContext.Outlets.AsNoTracking()
                on location.OutletId equals outlet.Id
            where balance.TenantId == tenantId && balance.ProductId == productId
            select new
            {
                outlet.Id,
                outlet.OutletName,
                outlet.OutletCode,
                balance.OnHandQuantity,
                balance.AvailableQuantity,
            })
            .ToListAsync(cancellationToken);

        var reorderRule = await _dbContext.InventoryReorderRules
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status == "ACTIVE")
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var trackInventory = balanceRows.Count > 0 || reorderRule is not null;

        var outlets = balanceRows
            .GroupBy(x => x.Id)
            .Select(group => new TenantAdminProductOutletDetailResponse(
                group.Key,
                group.First().OutletName,
                group.First().OutletCode,
                group.Sum(x => x.OnHandQuantity),
                group.Sum(x => x.AvailableQuantity)))
            .OrderBy(x => x.OutletName)
            .ToList();

        var totalOnHand = balanceRows.Sum(x => x.OnHandQuantity);
        var totalAvailable = balanceRows.Sum(x => x.AvailableQuantity);

        TenantAdminProductStockDetailResponse? stock = trackInventory
            ? new TenantAdminProductStockDetailResponse(
                totalOnHand > 0 ? totalOnHand : null,
                reorderRule?.MinStockQuantity ?? reorderRule?.ReorderPointQuantity,
                reorderRule?.MaxStockQuantity,
                string.IsNullOrWhiteSpace(unitType) ? null : unitType,
                totalOnHand,
                totalAvailable)
            : null;

        var variantDetails = variants
            .Select(variant =>
            {
                pricesByVariant.TryGetValue(variant.Id, out var price);
                barcodesByVariant.TryGetValue(variant.Id, out var barcode);

                return new TenantAdminProductVariantDetailResponse(
                    variant.Id,
                    variant.VariantName,
                    variant.Sku ?? string.Empty,
                    barcode,
                    price.SellingPrice,
                    price.CompareAtPrice,
                    variant.Status,
                    images.Where(image => image.ProductVariantId == variant.Id).ToList());
            })
            .ToList();

        var batch = await _dbContext.ProductBatches
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status == "ACTIVE")
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        TenantAdminProductBatchDetailResponse? batchDetails = batch is null
            ? null
            : new TenantAdminProductBatchDetailResponse(
                batch.BatchNumber,
                batch.ManufacturedAt,
                batch.ExpiryDate,
                null);

        var defaultBarcode = defaultVariant is not null &&
                             barcodesByVariant.TryGetValue(defaultVariant.Id, out var primaryBarcode)
            ? primaryBarcode
            : null;

        decimal defaultSellingPrice = 0m;
        decimal? defaultDiscountPrice = null;
        if (defaultVariant is not null && pricesByVariant.TryGetValue(defaultVariant.Id, out var defaultPrice))
        {
            defaultSellingPrice = defaultPrice.SellingPrice;
            defaultDiscountPrice = defaultPrice.CompareAtPrice;
        }

        return new TenantAdminProductDetailResponse(
            product.Id,
            product.ProductName,
            defaultVariant?.Sku ?? product.ProductCode,
            defaultBarcode,
            categoryId,
            categoryName,
            subCategory?.Id,
            product.BrandId,
            unitType,
            product.ShortDescription,
            imageUrl,
            productImages,
            CostPrice: null,
            defaultSellingPrice,
            defaultDiscountPrice,
            taxInfo?.Id,
            taxInfo?.TaxClassName,
            product.Status,
            trackInventory,
            stock,
            outlets,
            variantDetails,
            batchDetails,
            product.CreatedAt,
            product.UpdatedAt ?? product.CreatedAt);
    }

    public Task<bool> SkuExistsOnOtherProductAsync(
        Guid tenantId,
        string sku,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return _dbContext.ProductVariants
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Sku == sku &&
                     x.ProductId != productId &&
                     x.Status != ProductConstants.DeletedStatus,
                cancellationToken);
    }

    public Task<bool> BarcodeExistsOnOtherProductAsync(
        Guid tenantId,
        string barcode,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return _dbContext.ProductBarcodes
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Barcode == barcode &&
                     x.ProductId != productId,
                cancellationToken);
    }

    public async Task<TenantAdminProductDetailResponse?> UpdateProductAsync(
        Guid tenantId,
        Guid userId,
        Guid productId,
        TenantAdminProductCreateRequest request,
        Guid unitId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == productId &&
                     x.Status != ProductConstants.DeletedStatus,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        var normalizedCode = ProductConstants.NormalizeCode(request.Sku);
        var normalizedStatus = request.SaveAsDraft
            ? ProductConstants.InactiveStatus
            : ProductConstants.NormalizeStatus(request.Status);
        var slug = GenerateSlug(request.ProductName, normalizedCode);
        var productStructure = request.HasVariants ? "VARIANT" : "SIMPLE";

        product.UpdateProfile(
            normalizedCode,
            request.ProductName.Trim(),
            slug,
            "STANDARD",
            productStructure,
            businessTypeId: null,
            request.BrandId,
            returnPolicyId: null,
            request.ShortDescription,
            longDescription: null,
            isSellable: true,
            isTaxable: request.TaxId.HasValue,
            normalizedStatus,
            userId,
            now);

        var existingCategoryLinks = await _dbContext.ProductCategories
            .Where(x => x.TenantId == tenantId && x.ProductId == productId)
            .ToListAsync(cancellationToken);
        _dbContext.ProductCategories.RemoveRange(existingCategoryLinks);

        var categoryLinks = new List<ProductCategory>
        {
            ProductCategory.Create(
                Guid.NewGuid(),
                tenantId,
                productId,
                request.CategoryId,
                isPrimaryCategory: request.SubCategoryId == null,
                sortOrder: 0,
                userId,
                now),
        };

        if (request.SubCategoryId.HasValue)
        {
            categoryLinks.Add(ProductCategory.Create(
                Guid.NewGuid(),
                tenantId,
                productId,
                request.SubCategoryId.Value,
                isPrimaryCategory: true,
                sortOrder: 1,
                userId,
                now));
        }

        await _dbContext.ProductCategories.AddRangeAsync(categoryLinks, cancellationToken);

        var existingVariants = await _dbContext.ProductVariants
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status != ProductConstants.DeletedStatus)
            .ToListAsync(cancellationToken);

        var existingVariantsBySku = existingVariants
            .Where(x => !string.IsNullOrWhiteSpace(x.Sku))
            .GroupBy(x => x.Sku!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var variantDefinitions = BuildVariantDefinitions(request);
        var requestedSkus = variantDefinitions
            .Select(definition => definition.Sku)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Guid? defaultVariantId = null;
        var defaultPriceListId = await GetDefaultPriceListIdAsync(tenantId, cancellationToken);

        var existingBarcodes = await _dbContext.ProductBarcodes
            .Where(x => x.TenantId == tenantId && x.ProductId == productId)
            .ToListAsync(cancellationToken);
        _dbContext.ProductBarcodes.RemoveRange(existingBarcodes);

        for (var index = 0; index < variantDefinitions.Count; index++)
        {
            var definition = variantDefinitions[index];
            ProductVariant variant;

            if (existingVariantsBySku.TryGetValue(definition.Sku, out var existingVariant))
            {
                existingVariant.UpdateProfile(
                    definition.VariantCode,
                    definition.VariantName,
                    definition.Sku,
                    unitId,
                    unitId,
                    isDefaultVariant: index == 0,
                    isSellable: true,
                    allowFractionalQuantity: false,
                    normalizedStatus,
                    userId,
                    now);
                variant = existingVariant;
            }
            else
            {
                variant = ProductVariant.Create(
                    Guid.NewGuid(),
                    tenantId,
                    productId,
                    definition.VariantCode,
                    definition.VariantName,
                    definition.Sku,
                    unitId,
                    unitId,
                    isDefaultVariant: index == 0,
                    isSellable: true,
                    allowFractionalQuantity: false,
                    normalizedStatus,
                    userId,
                    now);
                await _dbContext.ProductVariants.AddAsync(variant, cancellationToken);
            }

            if (index == 0)
            {
                defaultVariantId = variant.Id;
            }

            if (!string.IsNullOrWhiteSpace(definition.Barcode))
            {
                var barcode = ProductBarcode.Create(
                    Guid.NewGuid(),
                    tenantId,
                    productId,
                    variant.Id,
                    definition.Barcode,
                    barcodeType: "EAN13",
                    uomId: null,
                    quantityPerScan: 1m,
                    isPrimaryBarcode: true,
                    status: "ACTIVE",
                    userId,
                    now);

                await _dbContext.ProductBarcodes.AddAsync(barcode, cancellationToken);
            }

            if (defaultPriceListId.HasValue)
            {
                var priceListItem = await _dbContext.PriceListItems
                    .FirstOrDefaultAsync(
                        x => x.TenantId == tenantId &&
                             x.PriceListId == defaultPriceListId.Value &&
                             x.ProductId == productId &&
                             x.ProductVariantId == variant.Id &&
                             x.Status != "DELETED",
                        cancellationToken);

                if (priceListItem is null)
                {
                    priceListItem = PriceListItem.Create(
                        Guid.NewGuid(),
                        tenantId,
                        defaultPriceListId.Value,
                        productId,
                        variant.Id,
                        unitId,
                        definition.SellingPrice,
                        definition.DiscountPrice,
                        minQuantity: 1m,
                        validFrom: null,
                        validUntil: null,
                        status: "ACTIVE",
                        userId,
                        now);
                    await _dbContext.PriceListItems.AddAsync(priceListItem, cancellationToken);
                }
                else
                {
                    priceListItem.UpdateProfile(
                        definition.SellingPrice,
                        definition.DiscountPrice,
                        minQuantity: 1m,
                        validFrom: null,
                        validUntil: null,
                        status: "ACTIVE",
                        userId,
                        now);
                }
            }
        }

        foreach (var existingVariant in existingVariants)
        {
            if (string.IsNullOrWhiteSpace(existingVariant.Sku) ||
                !requestedSkus.Contains(existingVariant.Sku))
            {
                existingVariant.SoftDelete(userId, now);
            }
        }

        var existingTaxAssignments = await _dbContext.ProductTaxAssignments
            .Where(x => x.TenantId == tenantId && x.ProductId == productId && x.Status != "DELETED")
            .ToListAsync(cancellationToken);

        foreach (var assignment in existingTaxAssignments)
        {
            assignment.SoftDelete(userId, now);
        }

        if (request.TaxId.HasValue && defaultVariantId.HasValue)
        {
            var taxAssignment = ProductTaxAssignment.Create(
                tenantId,
                productId,
                defaultVariantId,
                request.TaxId.Value,
                appliesFrom: null,
                appliesUntil: null,
                userId,
                now);

            await _dbContext.ProductTaxAssignments.AddAsync(taxAssignment, cancellationToken);
        }

        var existingBatch = await _dbContext.ProductBatches
            .Where(x => x.TenantId == tenantId && x.ProductId == productId && x.Status == "ACTIVE")
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        Guid? productBatchId = existingBatch?.Id;
        if (request.HasExpiryDate)
        {
            if (existingBatch is null)
            {
                var batch = ProductBatch.Create(
                    Guid.NewGuid(),
                    tenantId,
                    productId,
                    defaultVariantId,
                    request.BatchNumber!.Trim(),
                    supplierBatchNumber: null,
                    request.ManufactureDate,
                    request.ExpiryDate,
                    firstReceivedAt: now,
                    status: "ACTIVE",
                    userId,
                    now);

                await _dbContext.ProductBatches.AddAsync(batch, cancellationToken);
                productBatchId = batch.Id;
            }
            else
            {
                existingBatch.UpdateProfile(
                    supplierBatchNumber: null,
                    request.ManufactureDate,
                    request.ExpiryDate,
                    userId,
                    now);
            }
        }
        else if (existingBatch is not null)
        {
            existingBatch.UpdateStatus("INACTIVE", userId, now);
            productBatchId = null;
        }

        if (request.TrackInventory && request.OutletIds is { Count: > 0 })
        {
            foreach (var outletId in request.OutletIds.Distinct())
            {
                var inventoryLocationId = await _dbContext.InventoryLocations
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenantId &&
                        x.OutletId == outletId &&
                        x.Status == "ACTIVE")
                    .OrderByDescending(x => x.IsSellableLocation)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!inventoryLocationId.HasValue)
                {
                    continue;
                }

                var reorderRule = await _dbContext.InventoryReorderRules
                    .FirstOrDefaultAsync(
                        x => x.TenantId == tenantId &&
                             x.InventoryLocationId == inventoryLocationId.Value &&
                             x.ProductId == productId &&
                             x.Status == "ACTIVE",
                        cancellationToken);

                if (reorderRule is null)
                {
                    reorderRule = InventoryReorderRule.Create(
                        Guid.NewGuid(),
                        tenantId,
                        inventoryLocationId.Value,
                        productId,
                        defaultVariantId,
                        reorderMethod: "MIN_MAX",
                        reorderPointQuantity: request.MinimumStockAlertQuantity ?? 0m,
                        reorderQuantity: null,
                        minStockQuantity: request.MinimumStockAlertQuantity,
                        maxStockQuantity: request.MaximumStockQuantity,
                        safetyStockQuantity: 0m,
                        leadTimeDays: null,
                        supplierProductId: null,
                        isAutoReorder: false,
                        status: "ACTIVE",
                        userId,
                        now);
                    await _dbContext.InventoryReorderRules.AddAsync(reorderRule, cancellationToken);
                }
                else
                {
                    reorderRule.UpdateProfile(
                        reorderMethod: "MIN_MAX",
                        reorderPointQuantity: request.MinimumStockAlertQuantity ?? reorderRule.ReorderPointQuantity,
                        reorderQuantity: reorderRule.ReorderQuantity,
                        minStockQuantity: request.MinimumStockAlertQuantity,
                        maxStockQuantity: request.MaximumStockQuantity,
                        safetyStockQuantity: reorderRule.SafetyStockQuantity,
                        leadTimeDays: reorderRule.LeadTimeDays,
                        supplierProductId: reorderRule.SupplierProductId,
                        isAutoReorder: reorderRule.IsAutoReorder,
                        userId,
                        now);
                }

                if (request.OpeningStockQuantity.GetValueOrDefault() <= 0)
                {
                    continue;
                }

                var hasBalance = await _dbContext.InventoryBalances.AnyAsync(
                    x => x.TenantId == tenantId &&
                         x.InventoryLocationId == inventoryLocationId.Value &&
                         x.ProductId == productId,
                    cancellationToken);

                if (hasBalance)
                {
                    continue;
                }

                var quantityPerOutlet = request.OpeningStockQuantity!.Value / request.OutletIds.Count;
                var balance = InventoryBalance.Create(
                    Guid.NewGuid(),
                    tenantId,
                    inventoryLocationId.Value,
                    productId,
                    defaultVariantId,
                    productBatchId,
                    now);

                balance.AdjustQuantities(
                    quantityPerOutlet,
                    reservedDelta: 0,
                    damagedDelta: 0,
                    quarantineDelta: 0,
                    now);

                await _dbContext.InventoryBalances.AddAsync(balance, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(tenantId, productId, cancellationToken);
    }

    public async Task<TenantAdminProductActivationSnapshot?> GetActivationSnapshotAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == productId &&
                     x.Status != ProductConstants.DeletedStatus,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        var defaultVariant = await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status != ProductConstants.DeletedStatus)
            .OrderByDescending(x => x.IsDefaultVariant)
            .ThenBy(x => x.VariantCode)
            .FirstOrDefaultAsync(cancellationToken);

        var hasCategory = await _dbContext.ProductCategories
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId && x.ProductId == productId,
                cancellationToken);

        decimal sellingPrice = 0m;
        if (defaultVariant is not null)
        {
            var defaultPriceListId = await GetDefaultPriceListIdAsync(tenantId, cancellationToken);
            if (defaultPriceListId.HasValue)
            {
                sellingPrice = await _dbContext.PriceListItems
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenantId &&
                        x.PriceListId == defaultPriceListId.Value &&
                        x.ProductId == productId &&
                        x.ProductVariantId == defaultVariant.Id &&
                        x.Status != "DELETED")
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => x.SellingPrice)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        var unitType = string.Empty;
        if (defaultVariant is not null && defaultVariant.StockUomId != Guid.Empty)
        {
            unitType = await _dbContext.UnitOfMeasures
                .AsNoTracking()
                .Where(x => x.Id == defaultVariant.StockUomId)
                .Select(x => x.UomCode)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        }

        return new TenantAdminProductActivationSnapshot(
            product.ProductName,
            defaultVariant?.Sku,
            hasCategory,
            sellingPrice,
            unitType);
    }

    public async Task<TenantAdminProductStatusUpdateResponse?> UpdateProductStatusAsync(
        Guid tenantId,
        Guid userId,
        Guid productId,
        string status,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == productId &&
                     x.Status != ProductConstants.DeletedStatus,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        var normalizedStatus = ProductConstants.NormalizeStatus(status);
        product.UpdateStatus(normalizedStatus, userId, now);

        var variants = await _dbContext.ProductVariants
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status != ProductConstants.DeletedStatus)
            .ToListAsync(cancellationToken);

        foreach (var variant in variants)
        {
            variant.UpdateStatus(normalizedStatus, userId, now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TenantAdminProductStatusUpdateResponse(productId, normalizedStatus);
    }

    public async Task<TenantAdminProductDeleteHistoryFlags?> GetDeleteHistoryFlagsAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == productId &&
                     x.Status != ProductConstants.DeletedStatus,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        return await BuildDeleteHistoryFlagsAsync(tenantId, productId, product, cancellationToken);
    }

    public async Task<TenantAdminProductDeleteOperationResult> DeleteProductAsync(
        Guid tenantId,
        Guid userId,
        Guid productId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == productId, cancellationToken);

        if (product is null)
        {
            return new TenantAdminProductDeleteOperationResult(null, "product.not_found");
        }

        if (product.Status == ProductConstants.DeletedStatus)
        {
            return new TenantAdminProductDeleteOperationResult(null, "product.delete_blocked");
        }

        var historyFlags = await BuildDeleteHistoryFlagsAsync(tenantId, productId, product, cancellationToken);
        var hasHistory = historyFlags.HasSales ||
                         historyFlags.HasStockMovements ||
                         historyFlags.HasBatches ||
                         historyFlags.HasReturns ||
                         historyFlags.HasAuditHistory;

        var variants = await _dbContext.ProductVariants
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status != ProductConstants.DeletedStatus)
            .ToListAsync(cancellationToken);

        if (hasHistory)
        {
            product.UpdateStatus(ProductConstants.InactiveStatus, userId, now);
            foreach (var variant in variants)
            {
                variant.UpdateStatus(ProductConstants.InactiveStatus, userId, now);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new TenantAdminProductDeleteOperationResult(
                new TenantAdminProductDeleteResponse(
                    productId,
                    "Archived",
                    ProductConstants.InactiveStatus),
                null);
        }

        product.SoftDelete(userId, now);
        foreach (var variant in variants)
        {
            variant.SoftDelete(userId, now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TenantAdminProductDeleteOperationResult(
            new TenantAdminProductDeleteResponse(
                productId,
                "Deleted",
                ProductConstants.DeletedStatus),
            null);
    }

    private async Task<TenantAdminProductDeleteHistoryFlags> BuildDeleteHistoryFlagsAsync(
        Guid tenantId,
        Guid productId,
        Product product,
        CancellationToken cancellationToken)
    {
        var hasSales = await _dbContext.SalesOrderLines
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId && x.ProductId == productId,
                cancellationToken) ||
            await _dbContext.SalesOrderLineComponents
                .AsNoTracking()
                .AnyAsync(
                    x => x.TenantId == tenantId && x.ItemProductId == productId,
                    cancellationToken);

        var hasStockMovements =
            await _dbContext.StockMovements
                .AsNoTracking()
                .AnyAsync(
                    movement => movement.TenantId == tenantId &&
                                _dbContext.InventoryBalances.Any(balance =>
                                    balance.Id == movement.InventoryBalanceId &&
                                    balance.TenantId == tenantId &&
                                    balance.ProductId == productId),
                    cancellationToken) ||
            await _dbContext.StockAdjustmentLines
                .AsNoTracking()
                .AnyAsync(
                    x => x.TenantId == tenantId && x.ProductId == productId,
                    cancellationToken) ||
            await _dbContext.StockTransferLines
                .AsNoTracking()
                .AnyAsync(
                    x => x.TenantId == tenantId && x.ProductId == productId,
                    cancellationToken) ||
            await _dbContext.StocktakeLines
                .AsNoTracking()
                .AnyAsync(
                    x => x.TenantId == tenantId && x.ProductId == productId,
                    cancellationToken);

        var hasBatches = await _dbContext.ProductBatches
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId && x.ProductId == productId,
                cancellationToken);

        var hasReturns =
            await _dbContext.SalesReturnLines
                .AsNoTracking()
                .AnyAsync(
                    line => line.TenantId == tenantId &&
                            _dbContext.SalesOrderLines.Any(orderLine =>
                                orderLine.Id == line.SalesOrderLineId &&
                                orderLine.TenantId == tenantId &&
                                orderLine.ProductId == productId),
                    cancellationToken) ||
            await _dbContext.SalesExchangeLines
                .AsNoTracking()
                .AnyAsync(
                    x => x.TenantId == tenantId && x.ReplacementProductId == productId,
                    cancellationToken);

        var hasAuditHistory = product.UpdatedAt > product.CreatedAt;

        return new TenantAdminProductDeleteHistoryFlags(
            hasSales,
            hasStockMovements,
            hasBatches,
            hasReturns,
            hasAuditHistory);
    }

    public async Task<TenantAdminProductCreateResponse> CreateProductAsync(
        Guid tenantId,
        Guid userId,
        TenantAdminProductCreateRequest request,
        Guid unitId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var productId = Guid.NewGuid();
        var normalizedCode = ProductConstants.NormalizeCode(request.Sku);
        var normalizedStatus = request.SaveAsDraft
            ? ProductConstants.InactiveStatus
            : ProductConstants.NormalizeStatus(request.Status);
        var slug = GenerateSlug(request.ProductName, normalizedCode);
        var productStructure = request.HasVariants ? "VARIANT" : "SIMPLE";

        var product = Product.Create(
            productId,
            tenantId,
            normalizedCode,
            request.ProductName.Trim(),
            slug,
            "STANDARD",
            productStructure,
            businessTypeId: null,
            request.BrandId,
            returnPolicyId: null,
            request.ShortDescription,
            longDescription: null,
            isSellable: true,
            isTaxable: request.TaxId.HasValue,
            normalizedStatus,
            userId,
            now);

        await _dbContext.Products.AddAsync(product, cancellationToken);

        var categoryLinks = new List<ProductCategory>
        {
            ProductCategory.Create(
                Guid.NewGuid(),
                tenantId,
                productId,
                request.CategoryId,
                isPrimaryCategory: request.SubCategoryId == null,
                sortOrder: 0,
                userId,
                now),
        };

        if (request.SubCategoryId.HasValue)
        {
            categoryLinks.Add(ProductCategory.Create(
                Guid.NewGuid(),
                tenantId,
                productId,
                request.SubCategoryId.Value,
                isPrimaryCategory: true,
                sortOrder: 1,
                userId,
                now));
        }

        await _dbContext.ProductCategories.AddRangeAsync(categoryLinks, cancellationToken);

        Guid? defaultVariantId = null;
        var variantDefinitions = BuildVariantDefinitions(request);

        for (var index = 0; index < variantDefinitions.Count; index++)
        {
            var definition = variantDefinitions[index];
            var variantId = Guid.NewGuid();
            if (index == 0)
            {
                defaultVariantId = variantId;
            }

            var variant = ProductVariant.Create(
                variantId,
                tenantId,
                productId,
                definition.VariantCode,
                definition.VariantName,
                definition.Sku,
                unitId,
                unitId,
                isDefaultVariant: index == 0,
                isSellable: true,
                allowFractionalQuantity: false,
                normalizedStatus,
                userId,
                now);

            await _dbContext.ProductVariants.AddAsync(variant, cancellationToken);

            if (!string.IsNullOrWhiteSpace(definition.Barcode))
            {
                var barcode = ProductBarcode.Create(
                    Guid.NewGuid(),
                    tenantId,
                    productId,
                    variantId,
                    definition.Barcode,
                    barcodeType: "EAN13",
                    uomId: null,
                    quantityPerScan: 1m,
                    isPrimaryBarcode: true,
                    status: "ACTIVE",
                    userId,
                    now);

                await _dbContext.ProductBarcodes.AddAsync(barcode, cancellationToken);
            }

            var defaultPriceListId = await GetDefaultPriceListIdAsync(tenantId, cancellationToken);
            if (defaultPriceListId.HasValue)
            {
                var priceListItem = PriceListItem.Create(
                    Guid.NewGuid(),
                    tenantId,
                    defaultPriceListId.Value,
                    productId,
                    variantId,
                    unitId,
                    definition.SellingPrice,
                    definition.DiscountPrice,
                    minQuantity: 1m,
                    validFrom: null,
                    validUntil: null,
                    status: "ACTIVE",
                    userId,
                    now);

                await _dbContext.PriceListItems.AddAsync(priceListItem, cancellationToken);
            }
        }

        if (request.TaxId.HasValue && defaultVariantId.HasValue)
        {
            var taxAssignment = ProductTaxAssignment.Create(
                tenantId,
                productId,
                defaultVariantId,
                request.TaxId.Value,
                appliesFrom: null,
                appliesUntil: null,
                userId,
                now);

            await _dbContext.ProductTaxAssignments.AddAsync(taxAssignment, cancellationToken);
        }

        Guid? productBatchId = null;
        if (request.HasExpiryDate)
        {
            var batch = ProductBatch.Create(
                Guid.NewGuid(),
                tenantId,
                productId,
                defaultVariantId,
                request.BatchNumber!.Trim(),
                supplierBatchNumber: null,
                request.ManufactureDate,
                request.ExpiryDate,
                firstReceivedAt: now,
                status: "ACTIVE",
                userId,
                now);

            await _dbContext.ProductBatches.AddAsync(batch, cancellationToken);
            productBatchId = batch.Id;
        }

        if (request.TrackInventory &&
            request.OpeningStockQuantity.GetValueOrDefault() > 0 &&
            request.OutletIds is { Count: > 0 })
        {
            var quantityPerOutlet = request.OpeningStockQuantity!.Value / request.OutletIds.Count;
            foreach (var outletId in request.OutletIds.Distinct())
            {
                var inventoryLocationId = await _dbContext.InventoryLocations
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenantId &&
                        x.OutletId == outletId &&
                        x.Status == "ACTIVE")
                    .OrderByDescending(x => x.IsSellableLocation)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!inventoryLocationId.HasValue)
                {
                    continue;
                }

                var balance = InventoryBalance.Create(
                    Guid.NewGuid(),
                    tenantId,
                    inventoryLocationId.Value,
                    productId,
                    defaultVariantId,
                    productBatchId,
                    now);

                balance.AdjustQuantities(
                    quantityPerOutlet,
                    reservedDelta: 0,
                    damagedDelta: 0,
                    quarantineDelta: 0,
                    now);

                await _dbContext.InventoryBalances.AddAsync(balance, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TenantAdminProductCreateResponse(
            productId,
            request.ProductName.Trim(),
            request.Sku.Trim(),
            normalizedStatus);
    }

    private static List<VariantDefinition> BuildVariantDefinitions(TenantAdminProductCreateRequest request)
    {
        if (request.HasVariants && request.Variants is { Count: > 0 })
        {
            return request.Variants
                .Select((variant, index) => new VariantDefinition(
                    VariantCode: $"VARIANT-{index + 1}",
                    VariantName: string.IsNullOrWhiteSpace(variant.VariantName)
                        ? request.ProductName.Trim()
                        : variant.VariantName.Trim(),
                    variant.Sku.Trim(),
                    variant.Barcode?.Trim(),
                    variant.SellingPrice,
                    variant.DiscountPrice))
                .ToList();
        }

        return
        [
            new VariantDefinition(
                VariantCode: "DEFAULT",
                VariantName: request.ProductName.Trim(),
                request.Sku.Trim(),
                request.Barcode?.Trim(),
                request.SellingPrice,
                request.DiscountPrice),
        ];
    }

    private async Task<Guid?> GetDefaultPriceListIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.PriceLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string GenerateSlug(string name, string code)
    {
        var normalizedName = new string(name
            .Trim()
            .ToLowerInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_')
            .ToArray());

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            normalizedName = "product";
        }

        return $"{normalizedName}-{code.ToLowerInvariant()}";
    }

    private sealed record VariantDefinition(
        string VariantCode,
        string VariantName,
        string Sku,
        string? Barcode,
        decimal SellingPrice,
        decimal? DiscountPrice);
}
