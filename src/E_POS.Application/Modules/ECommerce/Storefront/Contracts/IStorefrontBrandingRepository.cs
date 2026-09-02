using E_POS.Application.Modules.ECommerce.Storefront.Dtos;

namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontBrandingRepository
{
    Task<StorefrontBrandingReadModel?> GetBrandingAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
