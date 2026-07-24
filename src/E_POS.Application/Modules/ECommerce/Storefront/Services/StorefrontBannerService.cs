using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;

namespace E_POS.Application.Modules.ECommerce.Storefront.Services;

public sealed class StorefrontBannerService : IStorefrontBannerService
{
    private readonly IStorefrontBannerRepository _repository;

    public StorefrontBannerService(IStorefrontBannerRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<StorefrontBannerReadModel>> GetActiveBannersAsync(Guid tenantId, string bannerType, CancellationToken cancellationToken = default)
    {
        return await _repository.GetActiveBannersAsync(tenantId, bannerType, cancellationToken);
    }
}