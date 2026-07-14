using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontProductRepository
{
    Task<StorefrontPagedReadModel<StorefrontProductListReadModel>> GetProductsAsync(Guid tenantId, Guid categoryId, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<StorefrontProductDetailReadModel?> GetProductDetailAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<(Product Product, ProductRatingSummary? Rating, decimal? SellingPrice, string? PrimaryImageUrl)>> GetBestSellersAsync(Guid tenantId, CancellationToken cancellationToken = default);
}