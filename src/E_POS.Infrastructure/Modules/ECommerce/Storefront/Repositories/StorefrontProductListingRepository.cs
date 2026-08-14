using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Application.Modules.ECommerce.Storefront.Mappers;
using E_POS.Application.Modules.Shared.Media;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;

public sealed class StorefrontProductListingRepository : StorefrontProductRepositoryBase, IStorefrontProductListingRepository
{
    public StorefrontProductListingRepository(EPosDbContext dbContext, IMediaReadUrlResolver? mediaReadUrlResolver = null)
        : base(dbContext, mediaReadUrlResolver)
    {
    }

    public async Task<StorefrontPagedReadModel<StorefrontProductListReadModel>> GetProductsAsync(
        Guid tenantId,
        Guid categoryId,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var productCategoryRows = await (
                from productCategory in DbContext.Set<ProductCategory>().AsNoTracking()
                join product in DbContext.Set<Product>().AsNoTracking()
                    on new { productCategory.TenantId, productCategory.ProductId }
                    equals new { product.TenantId, ProductId = product.Id }
                where productCategory.TenantId == tenantId &&
                      productCategory.CategoryId == categoryId &&
                      product.Status == ActiveStatus &&
                      product.IsSellable
                select new
                {
                    Product = product,
                    productCategory.SortOrder,
                    productCategory.IsPrimaryCategory
                })
            .ToListAsync(cancellationToken);

        var products = productCategoryRows
            .GroupBy(x => x.Product.Id)
            .Select(g => g.OrderByDescending(x => x.IsPrimaryCategory).ThenBy(x => x.SortOrder).First())
            .ToList();

        if (products.Count == 0)
        {
            return new StorefrontPagedReadModel<StorefrontProductListReadModel>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };
        }

        var allProductIds = products.Select(x => x.Product.Id).ToList();
        var now = DateTimeOffset.UtcNow;
        var currencyCode = await ResolveCurrencyCodeAsync(tenantId, cancellationToken);
        var normalizedSort = sort?.Trim().ToLowerInvariant();

        Dictionary<Guid, decimal?>? pricesByProduct = null;
        Dictionary<Guid, ProductRatingSummary>? ratingsByProduct = null;

        var needsPrices = normalizedSort == "price_asc" || normalizedSort == "price_desc";
        var needsRatings = normalizedSort == "rating_desc" || string.IsNullOrEmpty(normalizedSort) || (normalizedSort != "price_asc" && normalizedSort != "price_desc" && normalizedSort != "newest");

        if (needsPrices)
            pricesByProduct = await GetProductPricesByProductAsync(tenantId, allProductIds, currencyCode, now, cancellationToken);

        if (needsRatings)
            ratingsByProduct = await GetRatingsByProductAsync(tenantId, allProductIds, cancellationToken);

        var sortItems = products.Select(row =>
        {
            var pId = row.Product.Id;
            var price = pricesByProduct != null && pricesByProduct.TryGetValue(pId, out var p) ? p : null;
            var rating = ratingsByProduct != null && ratingsByProduct.TryGetValue(pId, out var r) ? r : null;

            return new ProductListingSortItem(
                row.Product,
                price,
                row.SortOrder,
                rating?.AverageRating ?? 0m,
                rating?.TotalReviews ?? 0);
        });

        var pagedSortItems = SortProductListings(sortItems, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var pagedProductIds = pagedSortItems.Select(x => x.Product.Id).ToList();
        
        pricesByProduct ??= await GetProductPricesByProductAsync(tenantId, pagedProductIds, currencyCode, now, cancellationToken);
        ratingsByProduct ??= await GetRatingsByProductAsync(tenantId, pagedProductIds, cancellationToken);
        
        var imagesByProduct = await GetPrimaryImagesByProductAsync(tenantId, pagedProductIds, cancellationToken);
        var inventoryByProduct = await GetInventoryByProductAsync(tenantId, pagedProductIds, cancellationToken);
        var optionsByProduct = await GetVariantOptionsByProductAsync(tenantId, pagedProductIds, cancellationToken);

        var items = pagedSortItems.Select(row =>
        {
            var product = row.Product;
            ratingsByProduct.TryGetValue(product.Id, out var rating);
            pricesByProduct.TryGetValue(product.Id, out var sellingPrice);
            imagesByProduct.TryGetValue(product.Id, out var primaryImageUrl);
            var hasInventory = inventoryByProduct.TryGetValue(product.Id, out var availableQuantity);
            var averageRating = rating?.AverageRating ?? 0m;
            var reviewCount = rating?.TotalReviews ?? 0;
            
            var productOptions = optionsByProduct.TryGetValue(product.Id, out var opts) ? opts : new ProductVariantOptions([], [], []);

            return StorefrontProductMapper.ToListReadModel(
                product,
                sellingPrice,
                primaryImageUrl,
                averageRating,
                reviewCount,
                !hasInventory || availableQuantity > 0m,
                currencyCode,
                BuildSelectableOptions(productOptions));
        }).ToList();

        return new StorefrontPagedReadModel<StorefrontProductListReadModel>
        {
            Items = items,
            TotalCount = products.Count,
            Page = page,
            PageSize = pageSize
        };
    }

}
