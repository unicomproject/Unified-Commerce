using E_POS.Application.Modules.ECommerce.Storefront.Dtos;

namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontBannerService
{
    Task<IEnumerable<StorefrontBannerReadModel>> GetActiveBannersAsync(Guid tenantId, string bannerType, CancellationToken cancellationToken = default);
}