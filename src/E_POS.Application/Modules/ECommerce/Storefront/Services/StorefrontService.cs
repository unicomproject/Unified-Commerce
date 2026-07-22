using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;

namespace E_POS.Application.Modules.ECommerce.Storefront.Services;

public sealed class StorefrontService : IStorefrontService
{
    private readonly IStorefrontBannerService _bannerService;
    private readonly IStorefrontCategoryService _categoryService;
    private readonly IStorefrontProductService _productService;
    private readonly IStorefrontFulfillmentService _fulfillmentService;
    private readonly IStorefrontTenantService _tenantService;

    public StorefrontService(
        IStorefrontBannerService bannerService,
        IStorefrontCategoryService categoryService,
        IStorefrontProductService productService,
        IStorefrontFulfillmentService fulfillmentService,
        IStorefrontTenantService tenantService)
    {
        _bannerService = bannerService;
        _categoryService = categoryService;
        _productService = productService;
        _fulfillmentService = fulfillmentService;
        _tenantService = tenantService;
    }

    public Task<IEnumerable<StorefrontBannerReadModel>> GetActiveBannersAsync(Guid tenantId, string bannerType, CancellationToken cancellationToken = default)
    {
        return _bannerService.GetActiveBannersAsync(tenantId, bannerType, cancellationToken);
    }

    public Task<IEnumerable<StorefrontCategoryReadModel>> GetFeaturedCategoriesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return _categoryService.GetFeaturedCategoriesAsync(tenantId, cancellationToken);
    }

    public Task<IEnumerable<StorefrontCategoryListReadModel>> GetRootCategoriesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return _categoryService.GetRootCategoriesAsync(tenantId, cancellationToken);
    }

    public Task<IEnumerable<StorefrontCategoryListReadModel>> GetChildCategoriesAsync(Guid tenantId, Guid parentCategoryId, CancellationToken cancellationToken = default)
    {
        return _categoryService.GetChildCategoriesAsync(tenantId, parentCategoryId, cancellationToken);
    }

    public Task<StorefrontCategoryListReadModel?> GetCategoryBySlugAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default)
    {
        return _categoryService.GetCategoryBySlugAsync(tenantId, slug, cancellationToken);
    }

    public Task<StorefrontPagedReadModel<StorefrontProductListReadModel>> GetProductsAsync(Guid tenantId, Guid categoryId, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return _productService.GetProductsAsync(tenantId, categoryId, sort, page, pageSize, cancellationToken);
    }

    public Task<StorefrontProductDetailReadModel?> GetProductDetailAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default)
    {
        return _productService.GetProductDetailAsync(tenantId, slug, cancellationToken);
    }

    public Task<IEnumerable<StorefrontProductReadModel>> GetBestSellersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return _productService.GetBestSellersAsync(tenantId, cancellationToken);
    }

    public Task<StorefrontSearchReadModel> SearchAsync(Guid tenantId, StorefrontSearchRequest request, CancellationToken cancellationToken = default)
    {
        return _productService.SearchAsync(tenantId, request, cancellationToken);
    }

    public Task<IEnumerable<StorefrontStoreReadModel>> GetAvailableStoresAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return _fulfillmentService.GetAvailableStoresAsync(tenantId, cancellationToken);
    }

    public Task<ApplicationResult<StorefrontCollectionOptionsReadModel>> GetCollectionOptionsAsync(
        Guid tenantId,
        Guid outletId,
        int days,
        CancellationToken cancellationToken = default)
    {
        return _fulfillmentService.GetCollectionOptionsAsync(tenantId, outletId, days, cancellationToken);
    }

    public Task<(Guid? TenantId, string? BaseCurrencyCode)> ResolveTenantAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _tenantService.ResolveTenantAsync(slug, cancellationToken);
    }
}
