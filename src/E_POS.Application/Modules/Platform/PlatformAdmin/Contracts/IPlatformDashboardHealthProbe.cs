using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformDashboardHealthProbe
{
    Task<PlatformDashboardSystemHealthDto> ProbeAsync(CancellationToken cancellationToken);
}
