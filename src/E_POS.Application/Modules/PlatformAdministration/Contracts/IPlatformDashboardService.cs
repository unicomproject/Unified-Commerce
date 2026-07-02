using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Dtos;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformDashboardService
{
    Task<ApplicationResult<PlatformDashboardResponse>> GetDashboardAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);
}
