using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.PlatformAdministration.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.PlatformAdministration.Repositories;

public sealed class PlatformAuthRepository : IPlatformAuthRepository
{
    private readonly EPosDbContext _dbContext;

    public PlatformAuthRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PlatformUser?> FindUserByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return _dbContext.PlatformUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetActivePermissionCodesAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        // Combine direct user permissions with active role-based permissions.
        var directPermissions =
            from userPermission in _dbContext.PlatformUserPermissions.AsNoTracking()
            join permission in _dbContext.PlatformPermissions.AsNoTracking()
                on userPermission.PlatformPermissionId equals permission.Id
            where userPermission.PlatformUserId == platformUserId &&
                  permission.Status == PlatformAuthConstants.ActiveStatus
            select permission.PermissionCode;

        var rolePermissions =
            from userRole in _dbContext.PlatformUserRoles.AsNoTracking()
            join role in _dbContext.PlatformRoles.AsNoTracking()
                on userRole.PlatformRoleId equals role.Id
            join rolePermission in _dbContext.PlatformRolePermissions.AsNoTracking()
                on role.Id equals rolePermission.PlatformRoleId
            join permission in _dbContext.PlatformPermissions.AsNoTracking()
                on rolePermission.PlatformPermissionId equals permission.Id
            where userRole.PlatformUserId == platformUserId &&
                  role.Status == PlatformAuthConstants.ActiveStatus &&
                  permission.Status == PlatformAuthConstants.ActiveStatus
            select permission.PermissionCode;

        return await directPermissions
            .Union(rolePermissions)
            .Where(x => x != string.Empty)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveFailedLoginAuditAsync(PlatformLoginAudit audit, CancellationToken cancellationToken)
    {
        _dbContext.PlatformLoginAudits.Add(audit);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveFailedCredentialAttemptAsync(
        PlatformLoginAudit audit,
        DateTimeOffset failedAttemptWindowStart,
        int maxFailedAttempts,
        CancellationToken cancellationToken)
    {
        _dbContext.PlatformLoginAudits.Add(audit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (audit.PlatformUserId is null)
        {
            return;
        }

        var failedAttempts = await _dbContext.PlatformLoginAudits
            .AsNoTracking()
            .CountAsync(
                x => x.PlatformUserId == audit.PlatformUserId &&
                     x.LoginResult == PlatformAuthConstants.FailedLoginResult &&
                     x.CreatedAt >= failedAttemptWindowStart,
                cancellationToken);

        if (failedAttempts < maxFailedAttempts)
        {
            return;
        }

        // Lock only active users so inactive/deleted account states are not overwritten by login noise.
        await _dbContext.PlatformUsers
            .Where(x => x.Id == audit.PlatformUserId && x.Status == PlatformAuthConstants.ActiveStatus)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, PlatformAuthConstants.LockedStatus)
                    .SetProperty(x => x.UpdatedAt, audit.CreatedAt),
                cancellationToken);
    }

    public async Task SaveSuccessfulLoginAsync(
        PlatformAuthSession session,
        PlatformRefreshToken refreshToken,
        PlatformLoginAudit audit,
        CancellationToken cancellationToken)
    {
        _dbContext.PlatformAuthSessions.Add(session);
        _dbContext.PlatformRefreshTokens.Add(refreshToken);
        _dbContext.PlatformLoginAudits.Add(audit);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}