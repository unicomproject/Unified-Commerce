using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformSettingsService
{
    Task<ApplicationResult<PlatformSettingsResponse>> GetSettingsAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformSettingsResponse>> UpdateSettingsAsync(
        UpdatePlatformSettingsRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken);
}

