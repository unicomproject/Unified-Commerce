using E_POS.Application.Modules.AuthSecurity.Contracts;
using E_POS.Domain.Modules.AuthSecurity.Constants;
using E_POS.Domain.Modules.AuthSecurity.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.AuthSecurity.Repositories;

public sealed class TenantAuthRepository : ITenantAuthRepository
{
    private readonly EPosDbContext _dbContext;

    public TenantAuthRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TenantLoginAccount?> FindLoginAccountByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        // Email is globally unique for tenant users, so tenant context is resolved server-side.
        return (
            from user in _dbContext.TenantUsers.AsNoTracking()
            join tenant in _dbContext.Tenants.AsNoTracking()
                on user.TenantId equals tenant.Id
            where user.NormalizedEmail == normalizedEmail
            select new TenantLoginAccount(
                user.Id,
                user.TenantId,
                user.NormalizedEmail,
                user.PasswordHash,
                user.Status,
                tenant.Status))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetActivePermissionCodesAsync(
        Guid tenantUserId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // Combine direct and role-based active permissions within the resolved tenant.
        var directPermissions =
            from userPermission in _dbContext.TenantUserPermissions.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking()
                on userPermission.TenantUserId equals user.Id
            join permission in _dbContext.PermissionDefinitions.AsNoTracking()
                on userPermission.PermissionDefinitionId equals permission.Id
            where user.Id == tenantUserId &&
                  user.TenantId == tenantId &&
                  permission.Status == TenantAuthConstants.ActiveUserStatus
            select permission.PermissionCode;

        var rolePermissions =
            from userRole in _dbContext.TenantUserRoles.AsNoTracking()
            join role in _dbContext.TenantRoles.AsNoTracking()
                on userRole.TenantRoleId equals role.Id
            join rolePermission in _dbContext.TenantRolePermissions.AsNoTracking()
                on role.Id equals rolePermission.TenantRoleId
            join permission in _dbContext.PermissionDefinitions.AsNoTracking()
                on rolePermission.PermissionDefinitionId equals permission.Id
            where userRole.TenantUserId == tenantUserId &&
                  role.TenantId == tenantId &&
                  role.Status == TenantAuthConstants.ActiveUserStatus &&
                  permission.Status == TenantAuthConstants.ActiveUserStatus
            select permission.PermissionCode;

        return await directPermissions
            .Union(rolePermissions)
            .Where(x => x != string.Empty)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveFailedLoginAuditAsync(TenantLoginAudit audit, CancellationToken cancellationToken)
    {
        _dbContext.TenantLoginAudits.Add(audit);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveFailedCredentialAttemptAsync(
        TenantLoginAudit audit,
        DateTimeOffset failedAttemptWindowStart,
        int maxFailedAttempts,
        CancellationToken cancellationToken)
    {
        _dbContext.TenantLoginAudits.Add(audit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (audit.TenantUserId is null)
        {
            return;
        }

        var failedAttempts = await _dbContext.TenantLoginAudits
            .AsNoTracking()
            .CountAsync(
                x => x.TenantUserId == audit.TenantUserId &&
                     x.LoginResult == TenantAuthConstants.FailedLoginResult &&
                     x.CreatedAt >= failedAttemptWindowStart,
                cancellationToken);

        if (failedAttempts < maxFailedAttempts)
        {
            return;
        }

        // Lock only active users so inactive/deleted account states are not overwritten by login noise.
        await _dbContext.TenantUsers
            .Where(x => x.Id == audit.TenantUserId && x.Status == TenantAuthConstants.ActiveUserStatus)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, TenantAuthConstants.LockedUserStatus)
                    .SetProperty(x => x.UpdatedAt, audit.CreatedAt),
                cancellationToken);
    }

    public async Task SaveSuccessfulLoginAsync(
        TenantAuthSession session,
        TenantRefreshToken refreshToken,
        TenantLoginAudit audit,
        CancellationToken cancellationToken)
    {
        _dbContext.TenantAuthSessions.Add(session);
        _dbContext.TenantRefreshTokens.Add(refreshToken);
        _dbContext.TenantLoginAudits.Add(audit);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
    public async Task RevokeCurrentSessionAsync(
        Guid tenantUserId,
        Guid tenantId,
        Guid sessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var session = await (
            from authSession in _dbContext.TenantAuthSessions
            join user in _dbContext.TenantUsers
                on authSession.TenantUserId equals user.Id
            where authSession.Id == sessionId &&
                  authSession.TenantUserId == tenantUserId &&
                  user.TenantId == tenantId
            select authSession)
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return;
        }

        session.Revoke(now);

        await _dbContext.TenantRefreshTokens
            .Where(x => x.TenantAuthSessionId == sessionId && x.Status == TenantAuthConstants.ActiveTokenStatus)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, TenantAuthConstants.RevokedTokenStatus)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
