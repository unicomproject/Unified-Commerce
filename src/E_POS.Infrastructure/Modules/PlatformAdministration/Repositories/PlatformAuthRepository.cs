using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.PlatformAdministration.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.PlatformAdministration.Repositories;

public sealed class PlatformAuthRepository : IPlatformAuthRepository
{
    private readonly EPosDbContext _dbContext;
    private readonly IPlatformPermissionRepository _permissionRepository;

    public PlatformAuthRepository(
        EPosDbContext dbContext,
        IPlatformPermissionRepository permissionRepository)
    {
        _dbContext = dbContext;
        _permissionRepository = permissionRepository;
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
        var permissionCodes = await _permissionRepository.GetActivePermissionCodesAsync(
            platformUserId,
            cancellationToken);

        return permissionCodes
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
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
    public async Task RevokeCurrentSessionAsync(
        Guid platformUserId,
        Guid sessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.PlatformAuthSessions
            .FirstOrDefaultAsync(
                x => x.Id == sessionId && x.PlatformUserId == platformUserId,
                cancellationToken);

        if (session is null)
        {
            return;
        }

        session.Revoke(now);

        var activeRefreshTokens = await _dbContext.PlatformRefreshTokens
            .Where(x => x.PlatformAuthSessionId == sessionId && x.Status == PlatformAuthConstants.ActiveTokenStatus)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.Revoke(now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PlatformAuthRefreshContext?> FindRefreshContextByTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken)
    {
        return await (
            from refreshToken in _dbContext.PlatformRefreshTokens.AsNoTracking()
            join session in _dbContext.PlatformAuthSessions.AsNoTracking()
                on refreshToken.PlatformAuthSessionId equals session.Id
            join user in _dbContext.PlatformUsers.AsNoTracking()
                on session.PlatformUserId equals user.Id
            where refreshToken.TokenHash == refreshTokenHash
            select new PlatformAuthRefreshContext
            {
                RefreshToken = refreshToken,
                Session = session,
                User = user
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> TryRotateRefreshTokenAsync(
        Guid refreshTokenId,
        PlatformRefreshToken replacementRefreshToken,
        string replacementSessionTokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var refreshToken = await _dbContext.PlatformRefreshTokens
            .FirstOrDefaultAsync(x => x.Id == refreshTokenId, cancellationToken);

        if (refreshToken is null || refreshToken.Status != PlatformAuthConstants.ActiveTokenStatus)
        {
            return false;
        }

        var session = await _dbContext.PlatformAuthSessions
            .FirstOrDefaultAsync(x => x.Id == refreshToken.PlatformAuthSessionId, cancellationToken);

        if (session is null || session.Status != PlatformAuthConstants.ActiveTokenStatus)
        {
            return false;
        }

        refreshToken.MarkUsed(now);
        session.RotateSessionToken(replacementSessionTokenHash, now);
        _dbContext.PlatformRefreshTokens.Add(replacementRefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
