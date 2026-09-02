namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontTenantRepository
{
    Task<(Guid? TenantId, string? BaseCurrencyCode)> GetTenantIdBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<(Guid? TenantId, string? BaseCurrencyCode)> GetTenantIdByHostAsync(string host, CancellationToken cancellationToken = default);
}
