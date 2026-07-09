using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;

public sealed class PlatformPasswordResetRepository : IPlatformPasswordResetRepository
{
    private readonly EPosDbContext _dbContext;

    public PlatformPasswordResetRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> PlatformUserExistsAsync(Guid platformUserId, CancellationToken cancellationToken)
    {
        return _dbContext.PlatformUsers
            .AsNoTracking()
            .AnyAsync(x => x.Id == platformUserId, cancellationToken);
    }

    public async Task AddPendingTokenAsync(PlatformPasswordResetToken token, CancellationToken cancellationToken)
    {
        _dbContext.PlatformPasswordResetTokens.Add(token);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<PlatformPasswordResetToken?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _dbContext.PlatformPasswordResetTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<bool> MarkUsedAsync(Guid tokenId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var token = await _dbContext.PlatformPasswordResetTokens
            .FirstOrDefaultAsync(x => x.Id == tokenId, cancellationToken);

        if (token is null || token.Status != PlatformAuthConstants.PendingTokenStatus)
        {
            return false;
        }

        token.MarkUsed(now);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> RevokeActivePendingTokensAsync(
        Guid platformUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var pendingTokens = await _dbContext.PlatformPasswordResetTokens
            .Where(x => x.PlatformUserId == platformUserId && x.Status == PlatformAuthConstants.PendingTokenStatus)
            .ToListAsync(cancellationToken);

        foreach (var token in pendingTokens)
        {
            token.Revoke(now);
        }

        if (pendingTokens.Count == 0)
        {
            return 0;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return pendingTokens.Count;
    }
}
