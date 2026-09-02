using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Persistence;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;

public sealed class StorefrontProductRepository : IStorefrontProductRepository
{
    private readonly IStorefrontProductListingRepository _listingRepository;
    private readonly IStorefrontProductDetailRepository _detailRepository;
    private readonly IStorefrontProductSearchRepository _searchRepository;
    private readonly IStorefrontProductBestSellerRepository _bestSellerRepository;

    public StorefrontProductRepository(EPosDbContext dbContext, IMediaReadUrlResolver? mediaReadUrlResolver = null)
        : this(
            new StorefrontProductListingRepository(dbContext, mediaReadUrlResolver),
            new StorefrontProductDetailRepository(dbContext, mediaReadUrlResolver),
            new StorefrontProductSearchRepository(dbContext, mediaReadUrlResolver),
            new StorefrontProductBestSellerRepository(dbContext, mediaReadUrlResolver))
    {
    }

    public StorefrontProductRepository(
        IStorefrontProductListingRepository listingRepository,
        IStorefrontProductDetailRepository detailRepository,
        IStorefrontProductSearchRepository searchRepository,
        IStorefrontProductBestSellerRepository bestSellerRepository)
    {
        _listingRepository = listingRepository;
        _detailRepository = detailRepository;
        _searchRepository = searchRepository;
        _bestSellerRepository = bestSellerRepository;
    }

    public Task<StorefrontPagedReadModel<StorefrontProductListReadModel>> GetProductsAsync(
        Guid tenantId,
        Guid categoryId,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        _listingRepository.GetProductsAsync(
            tenantId,
            categoryId,
            sort,
            page,
            pageSize,
            cancellationToken);

    public Task<StorefrontProductDetailReadModel?> GetProductDetailAsync(
        Guid tenantId,
        string slug,
        CancellationToken cancellationToken = default) =>
        _detailRepository.GetProductDetailAsync(tenantId, slug, cancellationToken);

    public Task<StorefrontSearchReadModel> SearchAsync(
        Guid tenantId,
        StorefrontSearchRequest request,
        CancellationToken cancellationToken = default) =>
        _searchRepository.SearchAsync(tenantId, request, cancellationToken);

    public Task<IEnumerable<(Product Product, ProductRatingSummary? Rating, decimal? SellingPrice, decimal? OriginalPrice, string CurrencyCode, string? PrimaryImageUrl)>> GetBestSellersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        _bestSellerRepository.GetBestSellersAsync(tenantId, cancellationToken);
}
