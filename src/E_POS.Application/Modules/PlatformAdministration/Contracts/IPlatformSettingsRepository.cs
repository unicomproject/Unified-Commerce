using E_POS.Application.Modules.PlatformAdministration.Dtos;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformSettingsRepository
{
    Task<PlatformSettingsResponse> GetGeneralSettingsAsync(CancellationToken cancellationToken);

    Task<PlatformSettingsResponse> SaveGeneralSettingsAsync(
        UpdatePlatformSettingsRequest request,
        Guid updatedByPlatformUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
