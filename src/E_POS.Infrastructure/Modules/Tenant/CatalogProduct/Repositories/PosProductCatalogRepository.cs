using E_POS.Application.Modules.Shared.Media;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed class PosProductCatalogRepository : IPosProductCatalogRepository
{
    private const string ActiveImageStatus = "ACTIVE";

    private readonly EPosDbContext _dbContext;

    public PosProductCatalogRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PosProductCatalogRepositoryResult> ListProductsAsync(
        Guid tenantId,
        Guid deviceId,
        Guid? categoryId,
        string? search,
        CancellationToken cancellationToken,
        Guid? outletId = null)
    {
        var deviceOutletId = await _dbContext.PosDevices
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == deviceId)
            .Select(x => (Guid?)x.OutletId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!deviceOutletId.HasValue)
        {
            return new PosProductCatalogRepositoryResult("pos_products.device_not_found", []);
        }

        var defaultPriceListId = await _dbContext.PriceLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var productsQuery = _dbContext.Products
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Status == ProductConstants.ActiveStatus &&
                x.IsSellable);

        if (categoryId is { } requestedCategoryId && requestedCategoryId != Guid.Empty)
        {
            productsQuery = productsQuery.Where(product =>
                _dbContext.ProductCategories.Any(link =>
                    link.TenantId == tenantId &&
                    link.ProductId == product.Id &&
                    link.CategoryId == requestedCategoryId));
        }

        var searchFilter = await ApplySearchFilterAsync(
            productsQuery,
            tenantId,
            search,
            cancellationToken);
        productsQuery = searchFilter.Products;
        var matchedVariantIds = searchFilter.MatchedVariantIds;

        var products = await productsQuery
            .OrderBy(x => x.ProductName)
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            return new PosProductCatalogRepositoryResult(null, []);
        }

        var productIds = products.Select(x => x.Id).ToList();
        var variants = await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                productIds.Contains(x.ProductId) &&
                x.Status == ProductConstants.ActiveStatus &&
                x.IsSellable)
            .ToListAsync(cancellationToken);

        var variantsByProduct = variants
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var variantIds = variants.Select(x => x.Id).ToList();
        var pricesByVariant = new Dictionary<Guid, decimal>();
        if (defaultPriceListId.HasValue && variantIds.Count > 0)
        {
            var priceRows = await _dbContext.PriceListItems
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.PriceListId == defaultPriceListId.Value &&
                    x.ProductVariantId.HasValue &&
                    variantIds.Contains(x.ProductVariantId.Value) &&
                    x.Status == "ACTIVE")
                .Select(x => new { VariantId = x.ProductVariantId!.Value, x.SellingPrice })
                .ToListAsync(cancellationToken);

            pricesByVariant = priceRows.ToDictionary(x => x.VariantId, x => x.SellingPrice);
        }

        var categoryRows = await (
                from link in _dbContext.ProductCategories.AsNoTracking()
                join category in _dbContext.Categories.AsNoTracking()
                    on link.CategoryId equals category.Id
                where link.TenantId == tenantId && productIds.Contains(link.ProductId)
                orderby link.IsPrimaryCategory descending, link.SortOrder
                select new { link.ProductId, link.CategoryId, category.CategoryName })
            .ToListAsync(cancellationToken);

        var categoryByProduct = categoryRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.First());

        var inventoryByVariant = new Dictionary<Guid, decimal>();
        if (variantIds.Count > 0)
        {
            var scopedOutletId = outletId is { } requestedOutletId && requestedOutletId != Guid.Empty
                ? requestedOutletId
                : deviceOutletId.Value;
            var inventoryRows = await (
                        from balance in _dbContext.InventoryBalances.AsNoTracking()
                        join location in _dbContext.InventoryLocations.AsNoTracking()
                            on balance.InventoryLocationId equals location.Id
                        where balance.TenantId == tenantId &&
                              location.TenantId == tenantId &&
                              location.OutletId == scopedOutletId &&
                              location.IsSellableLocation &&
                              location.Status == "ACTIVE" &&
                              balance.ProductVariantId.HasValue &&
                              variantIds.Contains(balance.ProductVariantId.Value)
                        group balance by balance.ProductVariantId!.Value
                        into groupRows
                        select new
                        {
                            VariantId = groupRows.Key,
                            AvailableQuantity = groupRows.Sum(x => x.AvailableQuantity),
                        })
                .ToListAsync(cancellationToken);

            inventoryByVariant = inventoryRows.ToDictionary(
                x => x.VariantId,
                x => x.AvailableQuantity);
        }

        var barcodeByVariant = new Dictionary<Guid, string>();
        if (variantIds.Count > 0)
        {
            var barcodeRows = await _dbContext.ProductBarcodes
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.ProductVariantId.HasValue &&
                    variantIds.Contains(x.ProductVariantId.Value) &&
                    x.Status == "ACTIVE")
                .OrderByDescending(x => x.IsPrimaryBarcode)
                .ThenBy(x => x.Id)
                .Select(x => new { VariantId = x.ProductVariantId!.Value, x.Barcode })
                .ToListAsync(cancellationToken);
            barcodeByVariant = barcodeRows
                .GroupBy(x => x.VariantId)
                .ToDictionary(g => g.Key, g => g.First().Barcode);
        }

        var imageRows = await (from image in _dbContext.ProductImages.AsNoTracking()
                               join mediaAsset in _dbContext.Set<MediaAsset>().AsNoTracking()
                                   on new { image.TenantId, MediaAssetId = image.MediaAssetId }
                                   equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                               from mediaAsset in mediaAssets.DefaultIfEmpty()
                               where image.TenantId == tenantId &&
                                     productIds.Contains(image.ProductId) &&
                                     image.Status == ActiveImageStatus
                               orderby image.IsPrimaryImage ? 0 : 1, image.SortOrder
                               select new
                               {
                                   image.ProductId,
                                   image.ImageStorageKey,
                                   image.ImageUrl,
                                   MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl
                               })
            .ToListAsync(cancellationToken);

        var imageByProduct = imageRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                x => x.Key,
                x => ResolveImageValue(x.First().MediaPublicUrl, x.First().ImageUrl, x.First().ImageStorageKey));
        var hiddenProductIds = await ResolveHiddenProductIdsAsync(
            tenantId,
            productIds,
            cancellationToken);

        var reorderRulesByProduct = new Dictionary<Guid, decimal?>();
        if (productIds.Count > 0)
        {
            var rules = await _dbContext.InventoryReorderRules
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && productIds.Contains(x.ProductId) && x.Status == "ACTIVE")
                .Select(x => new { x.ProductId, x.MinStockQuantity })
                .ToListAsync(cancellationToken);

            reorderRulesByProduct = rules
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault(x => x.MinStockQuantity.HasValue)?.MinStockQuantity);
        }

        var summaries = new List<PosProductSummaryResponseDto>(products.Count);
        foreach (var product in products)
        {
            if (hiddenProductIds.Contains(product.Id))
            {
                continue;
            }

            variantsByProduct.TryGetValue(product.Id, out var productVariants);
            productVariants ??= [];

            var hasVariants =
                !string.Equals(product.ProductStructure, "SIMPLE", StringComparison.OrdinalIgnoreCase) ||
                productVariants.Count > 1;

            var defaultVariant =
                productVariants.FirstOrDefault(x => x.IsDefaultVariant) ??
                productVariants.FirstOrDefault();

            decimal? minPrice = null;
            foreach (var variant in productVariants)
            {
                if (!pricesByVariant.TryGetValue(variant.Id, out var price))
                {
                    continue;
                }

                minPrice = minPrice.HasValue ? Math.Min(minPrice.Value, price) : price;
            }

            categoryByProduct.TryGetValue(product.Id, out var categoryInfo);
            imageByProduct.TryGetValue(product.Id, out var imageStorageKey);

            var availableQuantities = productVariants
                .Where(variant => inventoryByVariant.ContainsKey(variant.Id))
                .Select(variant => inventoryByVariant[variant.Id])
                .ToList();
            decimal? availableQuantity = availableQuantities.Count == 0
                ? null
                : availableQuantities.Sum(quantity => Math.Max(0m, quantity));

            reorderRulesByProduct.TryGetValue(product.Id, out var minStockQuantity);
            var stockStatus = ResolveProductStockStatus(
                availableQuantities,
                availableQuantity,
                minStockQuantity);

            var searchMatchedVariants = productVariants
                .Where(variant => matchedVariantIds.Contains(variant.Id))
                .ToList();

            if (searchMatchedVariants.Count > 0)
            {
                foreach (var matchedVariant in searchMatchedVariants)
                {
                    var matchedAvailableQuantity = inventoryByVariant.GetValueOrDefault(matchedVariant.Id);
                    var hasInventory = inventoryByVariant.ContainsKey(matchedVariant.Id);
                    var matchedStockStatus = ResolveStockStatus(
                        hasInventory ? matchedAvailableQuantity : null,
                        minStockQuantity);

                    summaries.Add(new PosProductSummaryResponseDto(
                        product.Id,
                        matchedVariant.Id,
                        product.ProductName,
                        product.ShortDescription,
                        imageStorageKey,
                        categoryInfo?.CategoryId,
                        string.IsNullOrWhiteSpace(categoryInfo?.CategoryName) ? "General" : categoryInfo!.CategoryName,
                        (int)Math.Round(
                            pricesByVariant.GetValueOrDefault(matchedVariant.Id),
                            MidpointRounding.AwayFromZero),
                        hasVariants,
                        matchedStockStatus,
                        hasInventory ? matchedAvailableQuantity : null,
                        matchedVariant.Sku,
                        barcodeByVariant.GetValueOrDefault(matchedVariant.Id),
                        matchedVariant.VariantName));
                }

                continue;
            }

            summaries.Add(new PosProductSummaryResponseDto(
                product.Id,
                hasVariants ? null : defaultVariant?.Id,
                product.ProductName,
                product.ShortDescription,
                imageStorageKey,
                categoryInfo?.CategoryId,
                string.IsNullOrWhiteSpace(categoryInfo?.CategoryName) ? "General" : categoryInfo!.CategoryName,
                (int)Math.Round(minPrice ?? 0m, MidpointRounding.AwayFromZero),
                hasVariants,
                stockStatus,
                availableQuantity,
                defaultVariant?.Sku,
                defaultVariant is null
                    ? null
                    : barcodeByVariant.GetValueOrDefault(defaultVariant.Id)));
        }

        return new PosProductCatalogRepositoryResult(null, summaries);
    }

    public async Task<PosBarcodeProductRepositoryResult> GetProductByBarcodeAsync(
        Guid tenantId,
        Guid deviceId,
        string barcode,
        CancellationToken cancellationToken)
    {
        var device = await _dbContext.PosDevices
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Id == deviceId &&
                x.Status == PosDeviceConstants.ActiveStatus &&
                x.IsTrusted)
            .Select(x => new { x.OutletId })
            .SingleOrDefaultAsync(cancellationToken);

        if (device is null)
        {
            return new PosBarcodeProductRepositoryResult("pos_device.invalid", null);
        }

        var outletIsActive = await _dbContext.Outlets
            .AsNoTracking()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.Id == device.OutletId &&
                x.Status == OutletConstants.ActiveStatus,
                cancellationToken);
        if (!outletIsActive)
        {
            return new PosBarcodeProductRepositoryResult("pos_device.invalid", null);
        }

        var matchedBarcode = await _dbContext.ProductBarcodes
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Barcode == barcode &&
                x.Status == ProductConstants.ActiveStatus)
            .Select(x => new
            {
                x.ProductId,
                x.ProductVariantId,
                x.Barcode,
                x.BarcodeType,
                x.QuantityPerScan,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (matchedBarcode is null)
        {
            return new PosBarcodeProductRepositoryResult("pos_barcode.not_found", null);
        }

        var product = await _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Id == matchedBarcode.ProductId,
                cancellationToken);
        if (product is null || product.Status != ProductConstants.ActiveStatus || !product.IsSellable)
        {
            return new PosBarcodeProductRepositoryResult("pos_product.unavailable", null);
        }

        var hiddenProductIds = await ResolveHiddenProductIdsAsync(
            tenantId,
            [product.Id],
            cancellationToken);
        if (hiddenProductIds.Contains(product.Id))
        {
            return new PosBarcodeProductRepositoryResult("pos_product.unavailable", null);
        }

        var variants = await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == product.Id &&
                x.Status == ProductConstants.ActiveStatus &&
                x.IsSellable)
            .ToListAsync(cancellationToken);

        var resolvedVariants = matchedBarcode.ProductVariantId.HasValue
            ? variants.Where(x => x.Id == matchedBarcode.ProductVariantId.Value).ToList()
            : variants;
        if (resolvedVariants.Count == 0)
        {
            return new PosBarcodeProductRepositoryResult("pos_variant.unavailable", null);
        }
        if (resolvedVariants.Count > 1)
        {
            return new PosBarcodeProductRepositoryResult("pos_barcode.ambiguous", null);
        }

        var variant = resolvedVariants[0];
        var defaultPriceListId = await _dbContext.PriceLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!defaultPriceListId.HasValue)
        {
            return new PosBarcodeProductRepositoryResult("pos_price.unavailable", null);
        }

        var price = await _dbContext.PriceListItems
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.PriceListId == defaultPriceListId.Value &&
                x.ProductVariantId == variant.Id &&
                x.Status == "ACTIVE")
            .Select(x => (decimal?)x.SellingPrice)
            .SingleOrDefaultAsync(cancellationToken);
        if (!price.HasValue)
        {
            return new PosBarcodeProductRepositoryResult("pos_price.unavailable", null);
        }

        var availableQuantity = await (
                from balance in _dbContext.InventoryBalances.AsNoTracking()
                join location in _dbContext.InventoryLocations.AsNoTracking()
                    on balance.InventoryLocationId equals location.Id
                where balance.TenantId == tenantId &&
                      location.TenantId == tenantId &&
                      location.OutletId == device.OutletId &&
                      location.IsSellableLocation &&
                      location.Status == "ACTIVE" &&
                      balance.ProductVariantId == variant.Id
                select (decimal?)balance.AvailableQuantity)
            .SumAsync(cancellationToken);

        var minStockQuantity = await _dbContext.InventoryReorderRules
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == product.Id && x.Status == "ACTIVE")
            .Select(x => x.MinStockQuantity)
            .FirstOrDefaultAsync(cancellationToken);

        return new PosBarcodeProductRepositoryResult(
            null,
            new PosBarcodeProductResponseDto(
                product.Id,
                variant.Id,
                matchedBarcode.Barcode,
                matchedBarcode.BarcodeType,
                product.ProductName,
                variant.VariantName,
                variant.Sku,
                matchedBarcode.QuantityPerScan,
                (int)Math.Round(price.Value, MidpointRounding.AwayFromZero),
                availableQuantity,
                ResolveStockStatus(availableQuantity, minStockQuantity)));
    }

    public async Task<PosProductCatalogCategoriesRepositoryResult> ListCategoriesAsync(
        Guid tenantId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var deviceOutletId = await _dbContext.PosDevices
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == deviceId)
            .Select(x => (Guid?)x.OutletId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!deviceOutletId.HasValue)
        {
            return new PosProductCatalogCategoriesRepositoryResult("pos_products.device_not_found", []);
        }

        var categories = await (
                from category in _dbContext.Categories.AsNoTracking()
                where category.TenantId == tenantId &&
                      category.Status == CategoryConstants.ActiveStatus &&
                      _dbContext.ProductCategories.Any(link =>
                          link.TenantId == tenantId &&
                          link.CategoryId == category.Id &&
                          _dbContext.Products.Any(product =>
                              product.TenantId == tenantId &&
                              product.Id == link.ProductId &&
                              product.Status == ProductConstants.ActiveStatus &&
                              product.IsSellable))
                orderby category.SortOrder, category.CategoryName
                select new PosCatalogCategoryResponseDto(category.Id, category.CategoryName))
            .ToListAsync(cancellationToken);

        return new PosProductCatalogCategoriesRepositoryResult(null, categories);
    }

    public async Task<PosProductDetailRepositoryResult> GetProductDetailAsync(
        Guid tenantId,
        Guid deviceId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var deviceOutletId = await _dbContext.PosDevices
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == deviceId)
            .Select(x => (Guid?)x.OutletId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!deviceOutletId.HasValue)
        {
            return new PosProductDetailRepositoryResult("pos_products.device_not_found", null);
        }

        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.Id == productId &&
                    x.Status == ProductConstants.ActiveStatus &&
                    x.IsSellable,
                cancellationToken);

        if (product is null)
        {
            return new PosProductDetailRepositoryResult("pos_products.product_not_found", null);
        }

        var hiddenProductIds = await ResolveHiddenProductIdsAsync(
            tenantId,
            [productId],
            cancellationToken);

        if (hiddenProductIds.Contains(productId))
        {
            return new PosProductDetailRepositoryResult("pos_products.product_not_found", null);
        }

        var variants = await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status == ProductConstants.ActiveStatus &&
                x.IsSellable)
            .OrderByDescending(x => x.IsDefaultVariant)
            .ThenBy(x => x.VariantName)
            .ToListAsync(cancellationToken);

        if (variants.Count == 0)
        {
            return new PosProductDetailRepositoryResult("pos_products.product_not_found", null);
        }

        var hasVariants =
            !string.Equals(product.ProductStructure, "SIMPLE", StringComparison.OrdinalIgnoreCase) ||
            variants.Count > 1;

        var defaultPriceListId = await _dbContext.PriceLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsDefaultPriceList && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var variantIds = variants.Select(x => x.Id).ToList();
        var pricesByVariant = new Dictionary<Guid, decimal>();
        if (defaultPriceListId.HasValue)
        {
            var priceRows = await _dbContext.PriceListItems
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.PriceListId == defaultPriceListId.Value &&
                    x.ProductVariantId.HasValue &&
                    variantIds.Contains(x.ProductVariantId.Value) &&
                    x.Status == "ACTIVE")
                .Select(x => new { VariantId = x.ProductVariantId!.Value, x.SellingPrice })
                .ToListAsync(cancellationToken);

            pricesByVariant = priceRows.ToDictionary(x => x.VariantId, x => x.SellingPrice);
        }

        var inventoryByVariant = new Dictionary<Guid, decimal>();
        var inventoryRows = await (
            from balance in _dbContext.InventoryBalances.AsNoTracking()
            join location in _dbContext.InventoryLocations.AsNoTracking()
                on balance.InventoryLocationId equals location.Id
            where balance.TenantId == tenantId &&
                  location.TenantId == tenantId &&
                  location.OutletId == deviceOutletId.Value &&
                  location.IsSellableLocation &&
                  location.Status == "ACTIVE" &&
                  balance.ProductVariantId.HasValue &&
                  variantIds.Contains(balance.ProductVariantId.Value)
            group balance by balance.ProductVariantId!.Value
            into groupRows
            select new
            {
                VariantId = groupRows.Key,
                AvailableQuantity = groupRows.Sum(x => x.AvailableQuantity),
            })
            .ToListAsync(cancellationToken);

        inventoryByVariant = inventoryRows.ToDictionary(x => x.VariantId, x => x.AvailableQuantity);

        var categoryName = await (
                from link in _dbContext.ProductCategories.AsNoTracking()
                join category in _dbContext.Categories.AsNoTracking()
                    on link.CategoryId equals category.Id
                where link.TenantId == tenantId && link.ProductId == productId
                orderby link.IsPrimaryCategory descending, link.SortOrder
                select category.CategoryName)
            .FirstOrDefaultAsync(cancellationToken) ?? "General";

        var imageStorageRow = await (from image in _dbContext.ProductImages.AsNoTracking()
                                     join mediaAsset in _dbContext.Set<MediaAsset>().AsNoTracking()
                                         on new { image.TenantId, MediaAssetId = image.MediaAssetId }
                                         equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                                     from mediaAsset in mediaAssets.DefaultIfEmpty()
                                     where image.TenantId == tenantId &&
                                           image.ProductId == productId &&
                                           image.Status == ActiveImageStatus
                                     orderby image.IsPrimaryImage ? 0 : 1, image.SortOrder
                                     select new
                                     {
                                         image.ImageStorageKey,
                                         image.ImageUrl,
                                         MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl
                                     })
            .FirstOrDefaultAsync(cancellationToken);

        var imageStorageKey = imageStorageRow is null
            ? null
            : ResolveImageValue(imageStorageRow.MediaPublicUrl, imageStorageRow.ImageUrl, imageStorageRow.ImageStorageKey);
        var productOptions = await _dbContext.ProductOptions
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductId == productId &&
                x.Status == "ACTIVE")
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var reorderRule = await _dbContext.InventoryReorderRules
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == productId && x.Status == "ACTIVE")
            .Select(x => x.MinStockQuantity)
            .FirstOrDefaultAsync(cancellationToken);

        var optionIds = productOptions.Select(x => x.Id).ToList();
        var optionValues = optionIds.Count == 0
            ? []
            : await _dbContext.ProductOptionValues
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    optionIds.Contains(x.ProductOptionId) &&
                    x.Status == "ACTIVE")
                .OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken);

        var variantOptionLinks = await _dbContext.ProductVariantOptionValues
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ProductId == productId)
            .ToListAsync(cancellationToken);

        var optionNameById = productOptions.ToDictionary(x => x.Id, x => x.OptionName);
        var optionValueNameById = optionValues.ToDictionary(x => x.Id, x => x.ValueName);

        var variantGroups = productOptions
            .Select(option => new PosProductVariantGroupResponseDto(
                option.OptionName,
                optionValues
                    .Where(value => value.ProductOptionId == option.Id)
                    .Select(value => value.ValueName)
                    .ToList()))
            .Where(group => group.Options.Count > 0)
            .ToList();

        var pricedVariants = variants
            .Where(variant => pricesByVariant.ContainsKey(variant.Id))
            .ToList();

        if (pricedVariants.Count == 0)
        {
            return new PosProductDetailRepositoryResult("pos_products.product_not_found", null);
        }

        decimal? minPrice = null;
        var variantDetails = new List<PosProductVariantDetailResponseDto>(pricedVariants.Count);
        foreach (var variant in pricedVariants)
        {
            var price = pricesByVariant[variant.Id];
            minPrice = minPrice.HasValue ? Math.Min(minPrice.Value, price) : price;

            decimal? availableQuantity = inventoryByVariant.TryGetValue(variant.Id, out var qty) ? qty : null;
            var stockStatus = ResolveStockStatus(availableQuantity, reorderRule);

            var attributes = variantOptionLinks
                .Where(link => link.ProductVariantId == variant.Id)
                .Select(link =>
                {
                    optionNameById.TryGetValue(link.ProductOptionId, out var optionName);
                    optionValueNameById.TryGetValue(link.ProductOptionValueId, out var valueName);
                    return new KeyValuePair<string, string>(
                        optionName ?? string.Empty,
                        valueName ?? string.Empty);
                })
                .Where(pair => pair.Key.Length > 0 && pair.Value.Length > 0)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            variantDetails.Add(new PosProductVariantDetailResponseDto(
                variant.Id,
                variant.Sku ?? string.Empty,
                (int)Math.Round(price, MidpointRounding.AwayFromZero),
                availableQuantity,
                stockStatus,
                attributes));
        }

        var productAvailableQuantities = variantDetails
            .Where(x => x.StockQty.HasValue)
            .Select(x => x.StockQty!.Value)
            .ToList();
        var productAvailableQuantity = productAvailableQuantities.Count > 0
            ? productAvailableQuantities.Sum(quantity => Math.Max(0m, quantity))
            : (decimal?)null;
        var detail = new PosProductDetailResponseDto(
            product.Id,
            product.ProductName,
            product.ShortDescription,
            imageStorageKey,
            categoryName,
            (int)Math.Round(minPrice ?? 0m, MidpointRounding.AwayFromZero),
            hasVariants,
            variantGroups,
            variantDetails,
            ResolveProductStockStatus(
                productAvailableQuantities,
                productAvailableQuantity,
                reorderRule),
            productAvailableQuantity);

        return new PosProductDetailRepositoryResult(null, detail);
    }

    private async Task<PosCatalogSearchFilter> ApplySearchFilterAsync(
        IQueryable<Domain.Modules.Tenant.CatalogProduct.Entities.Product> productsQuery,
        Guid tenantId,
        string? search,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return new PosCatalogSearchFilter(productsQuery, []);
        }

        var term = search.Trim();
        var normalizedTerm = term.ToUpperInvariant();

        var directMatches = await (
                from variant in _dbContext.ProductVariants.AsNoTracking()
                join product in _dbContext.Products.AsNoTracking()
                    on variant.ProductId equals product.Id
                where variant.TenantId == tenantId &&
                      product.TenantId == tenantId &&
                      variant.Status == ProductConstants.ActiveStatus &&
                      variant.IsSellable
                let skuMatch = variant.Sku != null && variant.Sku.ToUpper().Contains(normalizedTerm)
                let barcodeMatch = _dbContext.ProductBarcodes.Any(barcode =>
                    barcode.TenantId == tenantId &&
                    barcode.ProductVariantId == variant.Id &&
                    barcode.Status == ProductConstants.ActiveStatus &&
                    barcode.Barcode.ToUpper().Contains(normalizedTerm))
                let productVariantNameMatch =
                    (product.ProductName + " " + variant.VariantName).ToUpper() == normalizedTerm
                where skuMatch || barcodeMatch || productVariantNameMatch
                select new { variant.ProductId, VariantId = variant.Id })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (directMatches.Count > 0)
        {
            var directMatchProductIds = directMatches.Select(x => x.ProductId).Distinct().ToList();
            return new PosCatalogSearchFilter(
                productsQuery.Where(product => directMatchProductIds.Contains(product.Id)),
                directMatches.Select(x => x.VariantId).ToHashSet());
        }

        if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var pattern = $"%{term}%";
            return new PosCatalogSearchFilter(
                productsQuery.Where(product => EF.Functions.ILike(product.ProductName, pattern)),
                []);
        }

        return new PosCatalogSearchFilter(
            productsQuery.Where(product => product.ProductName.ToUpper().Contains(normalizedTerm)),
            []);
    }

    private sealed record PosCatalogSearchFilter(
        IQueryable<Domain.Modules.Tenant.CatalogProduct.Entities.Product> Products,
        HashSet<Guid> MatchedVariantIds);

    private async Task<HashSet<Guid>> ResolveHiddenProductIdsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var posChannelId = await (from s in _dbContext.SalesChannels.AsNoTracking()
                                  join p in _dbContext.PlatformSalesChannels.AsNoTracking() on s.PlatformSalesChannelId equals p.Id
                                  where s.TenantId == tenantId &&
                                        s.Status == "ACTIVE" &&
                                        (p.ChannelCode.ToUpper() == "POS" || p.ChannelType.ToUpper() == "PHYSICAL")
                                  orderby s.SortOrder
                                  select (Guid?)s.Id)
                                 .FirstOrDefaultAsync(cancellationToken);

        if (!posChannelId.HasValue)
        {
            return [];
        }

        var visibilityRows = await _dbContext.ProductChannelVisibilities
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);

        if (visibilityRows.Count == 0)
        {
            return [];
        }

        var hiddenProductIds = new HashSet<Guid>();
        foreach (var productId in visibilityRows.Select(x => x.ProductId).Distinct())
        {
            var productVisibility = visibilityRows
                .Where(x => x.ProductId == productId && x.SalesChannelId == posChannelId.Value)
                .ToList();

            if (productVisibility.Count == 0)
            {
                continue;
            }

            if (!productVisibility.Any(x => x.IsVisible && x.Status == ActiveImageStatus))
            {
                hiddenProductIds.Add(productId);
            }
        }

        return hiddenProductIds;
    }

    private static string ResolveStockStatus(decimal? availableQuantity, decimal? minStockQuantity)
    {
        if (!availableQuantity.HasValue)
        {
            return "in_stock";
        }

        if (availableQuantity.Value <= 0m)
        {
            return "out_of_stock";
        }

        var threshold = minStockQuantity ?? 5m;

        if (availableQuantity.Value <= threshold)
        {
            return "low_stock";
        }

        return "in_stock";
    }

    private static string ResolveProductStockStatus(
        IReadOnlyCollection<decimal> variantAvailableQuantities,
        decimal? totalAvailableQuantity,
        decimal? minStockQuantity)
    {
        if (variantAvailableQuantities.Count == 0)
        {
            return ResolveStockStatus(totalAvailableQuantity, minStockQuantity);
        }

        if (!variantAvailableQuantities.Any(quantity => quantity > 0m))
        {
            return "out_of_stock";
        }

        return ResolveStockStatus(totalAvailableQuantity, minStockQuantity);
    }

    private static string? ResolveImageValue(string? mediaPublicUrl, string? imageUrl, string imageStorageKey)
    {
        return MediaUrlResolver.PreferMediaAsset(mediaPublicUrl, imageUrl, imageStorageKey);
    }
}
