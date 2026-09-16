using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.TenantAuth.Dtos;

namespace E_POS.Application.Modules.Tenant.TenantAuth.Contracts;

public interface ITenantAuthService
{
    Task<ApplicationResult<TenantLoginResponse>> LoginAsync(
        TenantLoginRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TenantLoginResponse>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task<ApplicationResult> LogoutAsync(
        Guid tenantUserId,
        Guid tenantId,
        Guid sessionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Mints a short-lived, minimal-claims token for the notifications WebSocket handshake.
    /// The full access token carries a "permissions" claim that can grow to several KB for
    /// heavily-permissioned staff, which pushes the handshake request line (the token travels
    /// via query string, since browsers cannot set custom headers on a WebSocket upgrade)
    /// past Kestrel's/proxies' request-line length limit, failing with HTTP 414. This token
    /// drops the permissions claim entirely - the socket only registers a connection per user,
    /// it never authorizes an action - and expires in minutes rather than the full session.
    /// </summary>
    JwtTokenResult CreateNotificationSocketToken(Guid tenantUserId, Guid tenantId, Guid sessionId);
}
