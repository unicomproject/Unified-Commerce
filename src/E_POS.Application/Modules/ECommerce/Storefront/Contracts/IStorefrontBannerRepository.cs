using E_POS.Domain.Modules.ECommerce.Storefront.Entities;

namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontBannerRepository
{
    Task<IEnumerable<StorefrontBanner>> GetActiveBannersAsync(Guid tenantId, string bannerType, CancellationToken cancellationToken = default);
}