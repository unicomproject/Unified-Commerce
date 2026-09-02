using E_POS.Application.Modules.ECommerce.Storefront.Dtos;

namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontBrandingService
{
    Task<StorefrontBrandingReadModel?> GetBrandingAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
