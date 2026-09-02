namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontTenantService
{
    Task<(Guid? TenantId, string? BaseCurrencyCode)> ResolveTenantAsync(string slug, CancellationToken cancellationToken = default);
    Task<(Guid? TenantId, string? BaseCurrencyCode)> ResolveTenantByHostAsync(string host, CancellationToken cancellationToken = default);
}
