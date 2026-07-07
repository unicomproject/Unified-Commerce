using E_POS.Application.Common.Models;
using E_POS.Application.Modules.TenantAdministration.Dtos;

namespace E_POS.Application.Modules.TenantAdministration.Contracts;

public interface ITenantAdminContextService
{
    Task<ApplicationResult<TenantAdminContextDto>> GetContextAsync(
        Guid tenantUserId,
        Guid tenantId,
        CancellationToken cancellationToken);
}
