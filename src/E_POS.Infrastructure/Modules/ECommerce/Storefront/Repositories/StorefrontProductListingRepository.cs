using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Application.Modules.ECommerce.Storefront.Mappers;
using E_POS.Application.Modules.Shared.Media;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;

public sealed class StorefrontProductListingRepository : StorefrontProductRepositoryBase, IStorefrontProductListingRepository
{
    public StorefrontProductListingRepository(EPosDbContext dbContext) : base(dbContext)
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

        var productIds = products.Select(x => x.Product.Id).ToList();
        var now = DateTimeOffset.UtcNow;
        var currencyCode = await ResolveCurrencyCodeAsync(tenantId, cancellationToken);
        var ratingsByProduct = await GetRatingsByProductAsync(tenantId, productIds, cancellationToken);
        var pricesByProduct = await GetProductPricesByProductAsync(tenantId, productIds, currencyCode, now, cancellationToken);
        var imagesByProduct = await GetPrimaryImagesByProductAsync(tenantId, productIds, cancellationToken);
        var inventoryByProduct = await GetInventoryByProductAsync(tenantId, productIds, cancellationToken);

        var productModels = products.Select(row =>
        {
            var product = row.Product;
            ratingsByProduct.TryGetValue(product.Id, out var rating);
            pricesByProduct.TryGetValue(product.Id, out var sellingPrice);
            imagesByProduct.TryGetValue(product.Id, out var primaryImageUrl);
            var hasInventory = inventoryByProduct.TryGetValue(product.Id, out var availableQuantity);
            var averageRating = rating?.AverageRating ?? 0m;
            var reviewCount = rating?.TotalReviews ?? 0;

            return new ProductListingSortItem(
                StorefrontProductMapper.ToListReadModel(
                    product,
                    sellingPrice,
                    primaryImageUrl,
                    averageRating,
                    reviewCount,
                    !hasInventory || availableQuantity > 0m,
                    currencyCode),
                row.SortOrder,
                product.CreatedAt,
                averageRating,
                reviewCount);
        }).ToList();

        var items = SortProductListings(productModels, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Model)
            .ToList();

        return new StorefrontPagedReadModel<StorefrontProductListReadModel>
        {
            Items = items,
            TotalCount = productModels.Count,
            Page = page,
            PageSize = pageSize
        };
    }

}
