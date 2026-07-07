using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformAuthService
{
    Task<ApplicationResult<PlatformAdminLoginResponse>> LoginAsync(
        PlatformAdminLoginRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> LogoutAsync(
        Guid platformUserId,
        Guid sessionId,
        CancellationToken cancellationToken);

    Task LogoutByRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformAdminLoginResponse>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);
}
