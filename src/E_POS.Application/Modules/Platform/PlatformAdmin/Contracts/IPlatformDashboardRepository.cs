using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformDashboardRepository
{
    Task<PlatformDashboardResponse> GetDashboardAsync(
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken);
}

