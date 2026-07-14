using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosHomeDashboardService
{
    Task<ApplicationResult<PosHomeDashboardResponseDto>> GetPosHomeAsync(
        TenantRequestContext context,
        Guid? outletId,
        Guid? tillId,
        Guid? deviceId,
        string? deviceFingerprint,
        CancellationToken cancellationToken);
}
