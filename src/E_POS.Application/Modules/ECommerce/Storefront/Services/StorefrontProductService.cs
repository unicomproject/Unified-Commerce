using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Application.Modules.ECommerce.Storefront.Mappers;

namespace E_POS.Application.Modules.ECommerce.Storefront.Services;

public sealed class StorefrontProductService : IStorefrontProductService
{
    private readonly IStorefrontProductRepository _repository;

    public StorefrontProductService(IStorefrontProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<StorefrontPagedReadModel<StorefrontProductListReadModel>> GetProductsAsync(Guid tenantId, Guid categoryId, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _repository.GetProductsAsync(tenantId, categoryId, sort, page, pageSize, cancellationToken);
    }

    public async Task<StorefrontProductDetailReadModel?> GetProductDetailAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default)
    {
        return await _repository.GetProductDetailAsync(tenantId, slug, cancellationToken);
    }

    public async Task<IEnumerable<StorefrontProductReadModel>> GetBestSellersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var products = await _repository.GetBestSellersAsync(tenantId, cancellationToken);
        return products.ToBestSellerReadModels();
    }

    public Task<StorefrontSearchReadModel> SearchAsync(Guid tenantId, StorefrontSearchRequest request, CancellationToken cancellationToken = default)
        => _repository.SearchAsync(tenantId, request, cancellationToken);
}
