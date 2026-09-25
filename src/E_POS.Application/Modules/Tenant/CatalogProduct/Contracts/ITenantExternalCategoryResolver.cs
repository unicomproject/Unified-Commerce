using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ITenantExternalCategoryResolver
{
    Task<TenantCategoryResolutionResult> ResolveAsync(
        TenantCategoryResolutionRequest request,
        CancellationToken cancellationToken);
}
