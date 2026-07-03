using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Dtos;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

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
