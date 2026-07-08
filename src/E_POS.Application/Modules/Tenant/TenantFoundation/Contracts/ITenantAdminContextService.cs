using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;

namespace E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;

public interface ITenantAdminContextService
{
    Task<ApplicationResult<TenantAdminContextDto>> GetContextAsync(
        Guid tenantUserId,
        Guid tenantId,
        CancellationToken cancellationToken);
}

