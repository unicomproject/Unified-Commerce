using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed class PosProductCatalogRepository : IPosProductCatalogRepository
{
    private const string ActiveImageStatus = "ACTIVE";
    private const int LowStockThreshold = 5;

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
        CancellationToken cancellationToken)
    {
        var deviceExists = await _dbContext.PosDevices
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId && x.Id == deviceId,
                cancellationToken);

        if (!deviceExists)
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

        productsQuery = await ApplySearchFilterAsync(
            productsQuery,
            tenantId,
            search,
            cancellationToken);

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
                x.Status != ProductConstants.DeletedStatus &&
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

        var defaultVariantIds = products
            .Select(product =>
            {
                variantsByProduct.TryGetValue(product.Id, out var productVariants);
                productVariants ??= [];

                return productVariants.FirstOrDefault(x => x.IsDefaultVariant)?.Id ??
                       productVariants.FirstOrDefault()?.Id;
            })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        var inventoryByVariant = new Dictionary<Guid, decimal>();
        if (defaultVariantIds.Count > 0)
        {
            var inventoryRows = await _dbContext.InventoryBalances
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.ProductVariantId.HasValue &&
                    defaultVariantIds.Contains(x.ProductVariantId.Value))
                .GroupBy(x => x.ProductVariantId!.Value)
                .Select(group => new
                {
                    VariantId = group.Key,
                    AvailableQuantity = group.Sum(x => x.AvailableQuantity),
                })
                .ToListAsync(cancellationToken);

            inventoryByVariant = inventoryRows.ToDictionary(
                x => x.VariantId,
                x => x.AvailableQuantity);
        }

        var imageRows = await _dbContext.ProductImages
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                productIds.Contains(x.ProductId) &&
                x.Status == ActiveImageStatus)
            .OrderBy(x => x.IsPrimaryImage ? 0 : 1)
            .ThenBy(x => x.SortOrder)
            .Select(x => new { x.ProductId, x.ImageStorageKey, x.ImageUrl })
            .ToListAsync(cancellationToken);

        var imageByProduct = imageRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                x => x.Key,
                x => ResolveImageValue(x.First().ImageUrl, x.First().ImageStorageKey));

        var hiddenProductIds = await ResolveHiddenProductIdsAsync(
            tenantId,
            productIds,
            cancellationToken);

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

            decimal? availableQuantity = null;
            if (defaultVariant?.Id is { } defaultVariantId &&
                inventoryByVariant.TryGetValue(defaultVariantId, out var quantity))
            {
                availableQuantity = quantity;
            }

            var stockStatus = ResolveStockStatus(availableQuantity);

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
                availableQuantity));
        }

        return new PosProductCatalogRepositoryResult(null, summaries);
    }

    public async Task<PosProductCatalogCategoriesRepositoryResult> ListCategoriesAsync(
        Guid tenantId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var deviceExists = await _dbContext.PosDevices
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId && x.Id == deviceId,
                cancellationToken);

        if (!deviceExists)
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

    private async Task<IQueryable<Domain.Modules.Tenant.CatalogProduct.Entities.Product>> ApplySearchFilterAsync(
        IQueryable<Domain.Modules.Tenant.CatalogProduct.Entities.Product> productsQuery,
        Guid tenantId,
        string? search,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return productsQuery;
        }

        var term = search.Trim();
        var normalizedTerm = term.ToUpperInvariant();

        var directMatchProductIds = await (
                from variant in _dbContext.ProductVariants.AsNoTracking()
                where variant.TenantId == tenantId &&
                      variant.Status != ProductConstants.DeletedStatus
                let skuMatch = variant.Sku != null && variant.Sku.ToUpper() == normalizedTerm
                let barcodeMatch = _dbContext.ProductBarcodes.Any(barcode =>
                    barcode.TenantId == tenantId &&
                    barcode.ProductVariantId == variant.Id &&
                    barcode.Barcode.ToUpper() == normalizedTerm)
                where skuMatch || barcodeMatch
                select variant.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (directMatchProductIds.Count > 0)
        {
            return productsQuery.Where(product => directMatchProductIds.Contains(product.Id));
        }

        if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var pattern = $"%{term}%";
            return productsQuery.Where(product => EF.Functions.ILike(product.ProductName, pattern));
        }

        return productsQuery.Where(product => product.ProductName.ToUpper().Contains(normalizedTerm));
    }

    private async Task<HashSet<Guid>> ResolveHiddenProductIdsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var posChannelId = await _dbContext.SalesChannels
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Status == "ACTIVE" &&
                (x.ChannelCode.ToUpper() == "POS" || x.ChannelType.ToUpper() == "POS"))
            .OrderBy(x => x.SortOrder)
            .Select(x => (Guid?)x.Id)
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

    private static string ResolveStockStatus(decimal? availableQuantity)
    {
        if (!availableQuantity.HasValue)
        {
            return "in_stock";
        }

        if (availableQuantity.Value <= 0m)
        {
            return "out_of_stock";
        }

        if (availableQuantity.Value <= LowStockThreshold)
        {
            return "low_stock";
        }

        return "in_stock";
    }

    private static string? ResolveImageValue(string? imageUrl, string imageStorageKey)
    {
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            return imageUrl.Trim();
        }

        return string.IsNullOrWhiteSpace(imageStorageKey) ? null : imageStorageKey.Trim();
    }
}
