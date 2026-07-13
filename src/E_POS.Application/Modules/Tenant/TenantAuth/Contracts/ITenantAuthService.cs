using E_POS.Application.Common.Models;
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
}
