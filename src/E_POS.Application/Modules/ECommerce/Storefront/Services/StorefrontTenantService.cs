using E_POS.Application.Modules.ECommerce.Storefront.Contracts;

namespace E_POS.Application.Modules.ECommerce.Storefront.Services;

public sealed class StorefrontTenantService : IStorefrontTenantService
{
    private readonly IStorefrontTenantRepository _repository;

    public StorefrontTenantService(IStorefrontTenantRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid?> ResolveTenantIdAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _repository.GetTenantIdBySlugAsync(slug, cancellationToken);
    }
}