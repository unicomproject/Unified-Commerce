using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Dtos;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

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