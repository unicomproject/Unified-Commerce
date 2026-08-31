namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontTenantService
{
    Task<(Guid? TenantId, string? BaseCurrencyCode, string? StoreName, string? LogoUrl)> ResolveTenantAsync(string slug, CancellationToken cancellationToken = default);
}