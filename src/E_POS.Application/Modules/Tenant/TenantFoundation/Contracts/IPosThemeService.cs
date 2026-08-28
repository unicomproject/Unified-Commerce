using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;

namespace E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;

public interface IPosThemeService
{
    Task<ApplicationResult<PosThemeDto>> GetAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken);
}
