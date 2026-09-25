using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ITenantExternalBrandResolver
{
    Task<TenantBrandResolutionResult> ResolveAsync(
        TenantBrandResolutionRequest request,
        CancellationToken cancellationToken);
}
