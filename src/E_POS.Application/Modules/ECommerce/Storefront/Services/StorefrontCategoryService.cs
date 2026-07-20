using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Application.Modules.ECommerce.Storefront.Mappers;

namespace E_POS.Application.Modules.ECommerce.Storefront.Services;

public sealed class StorefrontCategoryService : IStorefrontCategoryService
{
    private readonly IStorefrontCategoryRepository _repository;

    public StorefrontCategoryService(IStorefrontCategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<StorefrontCategoryReadModel>> GetFeaturedCategoriesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var categories = await _repository.GetFeaturedCategoriesAsync(tenantId, cancellationToken);
        return categories.ToReadModels();
    }

    public async Task<IEnumerable<StorefrontCategoryListReadModel>> GetRootCategoriesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetRootCategoriesAsync(tenantId, cancellationToken);
    }

    public async Task<IEnumerable<StorefrontCategoryListReadModel>> GetChildCategoriesAsync(Guid tenantId, Guid parentCategoryId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetChildCategoriesAsync(tenantId, parentCategoryId, cancellationToken);
    }

    public async Task<StorefrontCategoryListReadModel?> GetCategoryBySlugAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default)
    {
        return await _repository.GetCategoryBySlugAsync(tenantId, slug, cancellationToken);
    }
}