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

public sealed class StorefrontProductBestSellerRepository : StorefrontProductRepositoryBase, IStorefrontProductBestSellerRepository
{
    public StorefrontProductBestSellerRepository(EPosDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<(Product Product, ProductRatingSummary? Rating, decimal? SellingPrice, string CurrencyCode, string? PrimaryImageUrl)>> GetBestSellersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var currencyCode = await ResolveCurrencyCodeAsync(tenantId, cancellationToken);

        var products = await DbContext.Set<Product>()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.Status == ActiveStatus && p.IsSellable)
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            return [];
        }

        var productIds = products.Select(p => p.Id).ToList();
        var ratingsByProduct = await GetRatingsByProductAsync(tenantId, productIds, cancellationToken);
        var pricesByProduct = await GetProductPricesByProductAsync(tenantId, productIds, currencyCode, now, cancellationToken);
        var imagesByProduct = await GetPrimaryImagesByProductAsync(tenantId, productIds, cancellationToken);

        return products.Select(product =>
        {
            ratingsByProduct.TryGetValue(product.Id, out var rating);
            pricesByProduct.TryGetValue(product.Id, out var sellingPrice);
            imagesByProduct.TryGetValue(product.Id, out var primaryImageUrl);
            return (product, rating, sellingPrice, currencyCode, primaryImageUrl);
        });
    }

}
