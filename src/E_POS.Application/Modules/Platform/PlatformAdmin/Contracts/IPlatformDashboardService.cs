using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformDashboardService
{
    Task<ApplicationResult<PlatformDashboardResponse>> GetDashboardAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);
}

