using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;

namespace E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;

public interface IDefaultTenantSettingsProvider
{
    Task<DefaultTenantSettingsProvisionResult> BuildAsync(
        DefaultTenantSettingsProvisionRequest request,
        CancellationToken cancellationToken);
}
