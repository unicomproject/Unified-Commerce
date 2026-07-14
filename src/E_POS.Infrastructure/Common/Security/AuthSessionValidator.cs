using System.Security.Claims;
using E_POS.Application.Common.Security;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Common.Security;

public sealed class AuthSessionValidator : IAuthSessionValidator
{
    private readonly EPosDbContext _dbContext;

    public AuthSessionValidator(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> IsCurrentSessionActiveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var identityType = principal.FindFirst("identity_type")?.Value;
        var sessionIdValue = principal.FindFirst("session_id")?.Value;

        if (!Guid.TryParse(sessionIdValue, out var sessionId))
        {
            return Task.FromResult(false);
        }

        return identityType switch
        {
            PlatformAuthConstants.IdentityType => IsPlatformSessionActiveAsync(principal, sessionId, cancellationToken),
            TenantAuthConstants.IdentityType => IsTenantSessionActiveAsync(principal, sessionId, cancellationToken),
            _ => Task.FromResult(false)
        };
    }

    private Task<bool> IsPlatformSessionActiveAsync(
        ClaimsPrincipal principal,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var platformUserIdValue = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(platformUserIdValue, out var platformUserId))
        {
            return Task.FromResult(false);
        }

        // Session validation also checks user status so locked/deactivated users lose access immediately.
        return (
            from session in _dbContext.PlatformAuthSessions.AsNoTracking()
            join user in _dbContext.PlatformUsers.AsNoTracking()
                on session.PlatformUserId equals user.Id
            where session.Id == sessionId &&
                  session.PlatformUserId == platformUserId &&
                  session.RevokedAt == null &&
                  user.Status == PlatformAuthConstants.ActiveStatus
            select session.Id)
            .AnyAsync(cancellationToken);
    }

    private Task<bool> IsTenantSessionActiveAsync(
        ClaimsPrincipal principal,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var tenantUserIdValue = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantIdValue = principal.FindFirst("tenant_id")?.Value;

        if (!Guid.TryParse(tenantUserIdValue, out var tenantUserId) ||
            !Guid.TryParse(tenantIdValue, out var tenantId))
        {
            return Task.FromResult(false);
        }

        // Tenant session validation joins user and tenant status so forged tenant_id claims and suspended tenants lose access immediately.
        // Note: IsTenantLoginStatusAllowed() cannot be used in EF LINQ queries; use inline constants instead.
        var activeTenantStatus = TenantAuthConstants.ActiveTenantStatus;
        var setupPendingTenantStatus = TenantAuthConstants.SetupPendingTenantStatus;

        return (
            from session in _dbContext.TenantAuthSessions.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking()
                on session.UserId equals user.Id
            join tenant in _dbContext.Tenants.AsNoTracking()
                on user.TenantId equals tenant.Id
            where session.Id == sessionId &&
                  session.UserId == tenantUserId &&
                  user.TenantId == tenantId &&
                  user.AccountStatus == TenantAuthConstants.ActiveUserStatus &&
                  (tenant.Status.ToLower() == activeTenantStatus ||
                   tenant.Status.ToLower() == setupPendingTenantStatus) &&
                  session.RevokedAt == null
            select session.Id)
            .AnyAsync(cancellationToken);
    }
}

