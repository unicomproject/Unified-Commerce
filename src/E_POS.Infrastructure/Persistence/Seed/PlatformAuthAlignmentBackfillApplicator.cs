using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Mirrors Phase 8A migration backfill rules for integration testing.
/// Does not invent IP, user-agent, device, risk score, or token values.
/// </summary>
public static class PlatformAuthAlignmentBackfillApplicator
{
    public static async Task ApplyAsync(
        EPosDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var sessions = await dbContext.PlatformAuthSessions.ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.ApplyAlignmentBackfill();
        }

        var sessionUserIds = sessions.ToDictionary(x => x.Id, x => x.PlatformUserId);

        var refreshTokens = await dbContext.PlatformRefreshTokens.ToListAsync(cancellationToken);
        foreach (var refreshToken in refreshTokens)
        {
            sessionUserIds.TryGetValue(refreshToken.PlatformAuthSessionId, out var platformUserId);
            refreshToken.ApplyAlignmentBackfill(platformUserId);
        }

        var passwordResetTokens = await dbContext.PlatformPasswordResetTokens.ToListAsync(cancellationToken);
        foreach (var token in passwordResetTokens)
        {
            token.ApplyAlignmentBackfill();
        }

        var loginAudits = await dbContext.PlatformLoginAudits.ToListAsync(cancellationToken);
        foreach (var audit in loginAudits)
        {
            audit.ApplyAlignmentBackfill();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
