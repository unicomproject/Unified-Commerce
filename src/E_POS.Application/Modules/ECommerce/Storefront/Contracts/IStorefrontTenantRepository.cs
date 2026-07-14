namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontTenantRepository
{
    Task<Guid?> GetTenantIdBySlugAsync(string slug, CancellationToken cancellationToken = default);
}