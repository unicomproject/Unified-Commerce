namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontTenantService
{
    Task<Guid?> ResolveTenantIdAsync(string slug, CancellationToken cancellationToken = default);
}