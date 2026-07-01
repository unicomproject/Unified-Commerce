using E_POS.Application.Common.Models;
using E_POS.Application.Modules.AuthSecurity.Dtos;

namespace E_POS.Application.Modules.AuthSecurity.Contracts;

public interface ITenantAuthService
{
    Task<ApplicationResult<TenantLoginResponse>> LoginAsync(
        TenantLoginRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> LogoutAsync(
        Guid tenantUserId,
        Guid tenantId,
        Guid sessionId,
        CancellationToken cancellationToken);
}