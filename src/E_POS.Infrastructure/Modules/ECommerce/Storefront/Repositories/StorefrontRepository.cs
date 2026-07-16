using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Domain.Modules.ECommerce.Storefront.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Persistence;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;

public sealed class StorefrontRepository : IStorefrontRepository
{
    private readonly IStorefrontBannerRepository _bannerRepository;
    private readonly IStorefrontCategoryRepository _categoryRepository;
    private readonly IStorefrontProductRepository _productRepository;
    private readonly IStorefrontFulfillmentRepository _fulfillmentRepository;
    private readonly IStorefrontTenantRepository _tenantRepository;

    public StorefrontRepository(
        IStorefrontBannerRepository bannerRepository,
        IStorefrontCategoryRepository categoryRepository,
        IStorefrontProductRepository productRepository,
        IStorefrontFulfillmentRepository fulfillmentRepository,
        IStorefrontTenantRepository tenantRepository)
    {
        _bannerRepository = bannerRepository;
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _fulfillmentRepository = fulfillmentRepository;
        _tenantRepository = tenantRepository;
    }

    public Task<IEnumerable<StorefrontBanner>> GetActiveBannersAsync(Guid tenantId, string bannerType, CancellationToken cancellationToken = default)
    {
        return _bannerRepository.GetActiveBannersAsync(tenantId, bannerType, cancellationToken);
    }

    public Task<IEnumerable<Category>> GetFeaturedCategoriesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return _categoryRepository.GetFeaturedCategoriesAsync(tenantId, cancellationToken);
    }

    public Task<IEnumerable<StorefrontCategoryListReadModel>> GetRootCategoriesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return _categoryRepository.GetRootCategoriesAsync(tenantId, cancellationToken);
    }

    public Task<IEnumerable<StorefrontCategoryListReadModel>> GetChildCategoriesAsync(Guid tenantId, Guid parentCategoryId, CancellationToken cancellationToken = default)
    {
        return _categoryRepository.GetChildCategoriesAsync(tenantId, parentCategoryId, cancellationToken);
    }

    public Task<StorefrontCategoryListReadModel?> GetCategoryBySlugAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default)
    {
        return _categoryRepository.GetCategoryBySlugAsync(tenantId, slug, cancellationToken);
    }

    public Task<StorefrontPagedReadModel<StorefrontProductListReadModel>> GetProductsAsync(Guid tenantId, Guid categoryId, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return _productRepository.GetProductsAsync(tenantId, categoryId, sort, page, pageSize, cancellationToken);
    }

    public Task<StorefrontProductDetailReadModel?> GetProductDetailAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default)
    {
        return _productRepository.GetProductDetailAsync(tenantId, slug, cancellationToken);
    }

    public Task<IEnumerable<(Product Product, ProductRatingSummary? Rating, decimal? SellingPrice, string? PrimaryImageUrl)>> GetBestSellersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return _productRepository.GetBestSellersAsync(tenantId, cancellationToken);
    }

    public Task<StorefrontSearchReadModel> SearchAsync(Guid tenantId, StorefrontSearchRequest request, CancellationToken cancellationToken = default)
    {
        return _productRepository.SearchAsync(tenantId, request, cancellationToken);
    }

    public Task<IEnumerable<StorefrontStoreReadModel>> GetAvailableStoresAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return _fulfillmentRepository.GetAvailableStoresAsync(tenantId, cancellationToken);
    }

    public Task<Guid?> GetTenantIdBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _tenantRepository.GetTenantIdBySlugAsync(slug, cancellationToken);
    }
}