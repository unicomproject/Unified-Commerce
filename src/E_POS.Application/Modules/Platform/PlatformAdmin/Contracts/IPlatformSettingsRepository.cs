using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformSettingsRepository
{
    Task<PlatformSettingsResponse> GetGeneralSettingsAsync(CancellationToken cancellationToken);

    Task<PlatformSettingsResponse> SaveGeneralSettingsAsync(
        UpdatePlatformSettingsRequest request,
        Guid updatedByPlatformUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

