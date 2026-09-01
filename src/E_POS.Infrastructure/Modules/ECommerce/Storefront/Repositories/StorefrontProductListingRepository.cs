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
        var now = DateTimeOffset.UtcNow;
        var currencyCode = await ResolveCurrencyCodeAsync(tenantId, cancellationToken);
        var normalizedSort = sort?.Trim().ToLowerInvariant();

        var baseQuery = from productCategory in DbContext.Set<ProductCategory>().AsNoTracking()
                        join product in DbContext.Set<Product>().AsNoTracking()
                            on new { productCategory.TenantId, productCategory.ProductId }
                            equals new { product.TenantId, ProductId = product.Id }
                        where productCategory.TenantId == tenantId &&
                              productCategory.CategoryId == categoryId &&
                              product.Status == ActiveStatus &&
                              product.IsSellable
                        select new { ProductCategory = productCategory, Product = product };

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return new StorefrontPagedReadModel<StorefrontProductListReadModel>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };
        }

        var queryWithSortingVars = from p in baseQuery
                                   let price = (from item in DbContext.Set<PriceListItem>().AsNoTracking()
                                                join priceList in DbContext.Set<PriceList>().AsNoTracking()
                                                    on new { item.TenantId, item.PriceListId } equals new { priceList.TenantId, PriceListId = priceList.Id }
                                                where item.TenantId == tenantId &&
                                                      item.ProductId == p.Product.Id &&
                                                      item.ProductVariantId == null &&
                                                      item.Status == ActiveStatus &&
                                                      item.MinQuantity <= 1m &&
                                                      priceList.Status == ActiveStatus &&
                                                      priceList.CurrencyCode == currencyCode &&
                                                      (!priceList.ValidFrom.HasValue || priceList.ValidFrom <= now) &&
                                                      (!priceList.ValidUntil.HasValue || priceList.ValidUntil >= now) &&
                                                      (!item.ValidFrom.HasValue || item.ValidFrom <= now) &&
                                                      (!item.ValidUntil.HasValue || item.ValidUntil >= now)
                                                orderby priceList.IsDefaultPriceList descending,
                                                        priceList.Priority descending,
                                                        item.ValidFrom ?? DateTimeOffset.MinValue descending,
                                                        item.MinQuantity descending
                                                select (decimal?)item.SellingPrice).FirstOrDefault()
                                   let rating = DbContext.Set<ProductRatingSummary>().AsNoTracking()
                                                   .Where(r => r.TenantId == tenantId && r.ProductId == p.Product.Id)
                                                   .FirstOrDefault()
                                   select new { p.Product, p.ProductCategory, Price = price, Rating = rating };

        IQueryable<Product> orderedQuery;
        if (normalizedSort == "price_asc")
        {
            orderedQuery = queryWithSortingVars
                .OrderBy(x => x.Price == null ? decimal.MaxValue : x.Price)
                .ThenBy(x => x.Product.ProductName)
                .Select(x => x.Product);
        }
        else if (normalizedSort == "price_desc")
        {
            orderedQuery = queryWithSortingVars
                .OrderByDescending(x => x.Price == null ? decimal.MinValue : x.Price)
                .ThenBy(x => x.Product.ProductName)
                .Select(x => x.Product);
        }
        else if (normalizedSort == "newest")
        {
            orderedQuery = queryWithSortingVars
                .OrderByDescending(x => x.Product.CreatedAt)
                .ThenBy(x => x.Product.ProductName)
                .Select(x => x.Product);
        }
        else // default sort
        {
            orderedQuery = queryWithSortingVars
                .OrderByDescending(x => x.ProductCategory.IsPrimaryCategory)
                .ThenBy(x => x.ProductCategory.SortOrder)
                .ThenByDescending(x => x.Rating != null ? x.Rating.TotalReviews : 0)
                .ThenByDescending(x => x.Rating != null ? x.Rating.AverageRating : 0m)
                .ThenBy(x => x.Product.ProductName)
                .Select(x => x.Product);
        }

        var pagedProducts = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var pagedProductIds = pagedProducts.Select(x => x.Id).ToList();

        var pricesByProduct = await GetProductPricesByProductAsync(tenantId, pagedProductIds, currencyCode, now, cancellationToken);
        var ratingsByProduct = await GetRatingsByProductAsync(tenantId, pagedProductIds, cancellationToken);
        var imagesByProduct = await GetPrimaryImagesByProductAsync(tenantId, pagedProductIds, cancellationToken);
        var inventoryByProduct = await GetInventoryByProductAsync(tenantId, pagedProductIds, cancellationToken);
        var optionsByProduct = await GetVariantOptionsByProductAsync(tenantId, pagedProductIds, cancellationToken);

        var items = pagedProducts.Select(product =>
        {
            ratingsByProduct.TryGetValue(product.Id, out var rating);
            var prices = pricesByProduct.TryGetValue(product.Id, out var p) ? p : (null, null);
            imagesByProduct.TryGetValue(product.Id, out var primaryImageUrl);
            var hasInventory = inventoryByProduct.TryGetValue(product.Id, out var availableQuantity);
            var averageRating = rating?.AverageRating ?? 0m;
            var reviewCount = rating?.TotalReviews ?? 0;
            
            var productOptions = optionsByProduct.TryGetValue(product.Id, out var opts) ? opts : new ProductVariantOptions([], [], []);

            return StorefrontProductMapper.ToListReadModel(
                product,
                prices.SellingPrice,
                prices.OriginalPrice,
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
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

}
