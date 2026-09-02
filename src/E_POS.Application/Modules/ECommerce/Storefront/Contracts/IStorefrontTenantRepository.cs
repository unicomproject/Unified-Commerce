namespace E_POS.Application.Modules.ECommerce.Storefront.Contracts;

public interface IStorefrontTenantRepository
{
    Task<(Guid? TenantId, string? BaseCurrencyCode, string? StoreName, string? LogoUrl)> GetTenantIdBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
