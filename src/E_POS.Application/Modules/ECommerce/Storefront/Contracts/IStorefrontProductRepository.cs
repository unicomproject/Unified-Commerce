using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontProductRepository :
    IStorefrontProductListingRepository,
    IStorefrontProductDetailRepository,
    IStorefrontProductSearchRepository,
    IStorefrontProductBestSellerRepository
{
}

public interface IStorefrontProductListingRepository
{
    Task<StorefrontPagedReadModel<StorefrontProductListReadModel>> GetProductsAsync(
        Guid tenantId,
        Guid categoryId,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface IStorefrontProductDetailRepository
{
    Task<StorefrontProductDetailReadModel?> GetProductDetailAsync(
        Guid tenantId,
        string slug,
        CancellationToken cancellationToken = default);
}

public interface IStorefrontProductSearchRepository
{
    Task<StorefrontSearchReadModel> SearchAsync(
        Guid tenantId,
        StorefrontSearchRequest request,
        CancellationToken cancellationToken = default);
}

public interface IStorefrontProductBestSellerRepository
{
    Task<IEnumerable<(Product Product, ProductRatingSummary? Rating, decimal? SellingPrice, decimal? OriginalPrice, string CurrencyCode, string? PrimaryImageUrl)>> GetBestSellersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}