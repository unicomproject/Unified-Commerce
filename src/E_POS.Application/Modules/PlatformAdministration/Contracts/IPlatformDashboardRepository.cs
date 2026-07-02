using E_POS.Application.Modules.PlatformAdministration.Dtos;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformDashboardRepository
{
    Task<PlatformDashboardResponse> GetDashboardAsync(
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken);
}
