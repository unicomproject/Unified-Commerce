using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;

namespace E_POS.Application.Modules.ECommerce.Storefront.Services;

public sealed class StorefrontBrandingService : IStorefrontBrandingService
{
    private readonly IStorefrontBrandingRepository _repository;

    public StorefrontBrandingService(IStorefrontBrandingRepository repository)
    {
        _repository = repository;
    }

    public Task<StorefrontBrandingReadModel?> GetBrandingAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        _repository.GetBrandingAsync(tenantId, cancellationToken);
}
