using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.TenantAuth.Repositories;

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
            where user.Email == normalizedEmail
            select new TenantLoginAccount(
                user.Id,
                user.TenantId,
                user.Email,
                user.EncryptedPassword, // it was PasswordHash, assuming it's EncryptedPassword in new ERD
                user.AccountStatus,
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
                  permission.IsActive
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
                  role.IsActive &&
                  permission.IsActive
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

        if (audit.UserId is null)
        {
            return;
        }

        var failedAttempts = await _dbContext.TenantLoginAudits
            .AsNoTracking()
            .CountAsync(
                x => x.UserId == audit.UserId &&
                     x.LoginStatus == TenantAuthConstants.FailedLoginResult &&
                     x.CreatedAt >= failedAttemptWindowStart,
                cancellationToken);

        if (failedAttempts < maxFailedAttempts)
        {
            return;
        }

        // Lock only active users so inactive/deleted account states are not overwritten by login noise.
        await _dbContext.TenantUsers
            .Where(x => x.Id == audit.UserId && x.AccountStatus == TenantAuthConstants.ActiveUserStatus)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.AccountStatus, TenantAuthConstants.LockedUserStatus)
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

    public async Task<TenantRefreshRotationResult> RotateRefreshTokenAsync(
        string currentTokenHash,
        Guid replacementTokenId,
        string replacementTokenHash,
        DateTimeOffset replacementExpiresAt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);

        var current = await _dbContext.TenantRefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == currentTokenHash, cancellationToken);

        if (current is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(TenantRefreshRotationStatus.Invalid, null, null);
        }

        var session = await _dbContext.TenantAuthSessions
            .SingleOrDefaultAsync(
                x => x.Id == current.TenantAuthSessionId &&
                     x.TenantId == current.TenantId &&
                     x.UserId == current.UserId,
                cancellationToken);

        if (current.UsedAt.HasValue || current.ReplacedByTokenId.HasValue)
        {
            if (session is not null)
            {
                session.Revoke(now, reason: "refresh_token_reuse");
            }

            await _dbContext.TenantRefreshTokens
                .Where(x => x.TokenFamilyId == current.TokenFamilyId && x.RevokedAt == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.RevokedAt, now)
                        .SetProperty(x => x.RevokeReason, "refresh_token_reuse")
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(TenantRefreshRotationStatus.Reused, null, null);
        }

        if (current.RevokedAt.HasValue ||
            current.ExpiresAt <= now ||
            session is null ||
            session.RevokedAt.HasValue ||
            session.ExpiresAt <= now)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(TenantRefreshRotationStatus.Invalid, null, null);
        }

        var account = await FindRefreshAccountAsync(current.UserId, current.TenantId, cancellationToken);
        if (account is null ||
            !string.Equals(account.UserStatus, TenantAuthConstants.ActiveUserStatus, StringComparison.OrdinalIgnoreCase) ||
            !TenantAuthConstants.IsTenantLoginStatusAllowed(account.TenantStatus))
        {
            session.Revoke(now, reason: "account_unavailable");
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(TenantRefreshRotationStatus.AccountUnavailable, null, null);
        }

        current.MarkRotated(replacementTokenId, now);
        session.Extend(replacementExpiresAt, now);
        _dbContext.TenantRefreshTokens.Add(TenantRefreshToken.Create(
            replacementTokenId,
            current.TenantId,
            current.TenantAuthSessionId,
            current.UserId,
            replacementTokenHash,
            current.TokenFamilyId,
            replacementExpiresAt,
            now));

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(TenantRefreshRotationStatus.Succeeded, account, current.TenantAuthSessionId);
    }

    private Task<TenantLoginAccount?> FindRefreshAccountAsync(
        Guid tenantUserId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return (
            from user in _dbContext.TenantUsers.AsNoTracking()
            join tenant in _dbContext.Tenants.AsNoTracking() on user.TenantId equals tenant.Id
            where user.Id == tenantUserId && user.TenantId == tenantId
            select new TenantLoginAccount(
                user.Id,
                user.TenantId,
                user.Email,
                user.EncryptedPassword,
                user.AccountStatus,
                tenant.Status))
            .SingleOrDefaultAsync(cancellationToken);
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
                on authSession.UserId equals user.Id
            where authSession.Id == sessionId &&
                  authSession.UserId == tenantUserId &&
                  user.TenantId == tenantId
            select authSession)
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return;
        }

        session.Revoke(now);

        await _dbContext.TenantRefreshTokens
            .Where(x => x.TenantAuthSessionId == sessionId && x.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}



