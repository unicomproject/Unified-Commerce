using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Mirrors Phase 8G-b migration backfill rules for integration testing.
/// Re-applies Phase 8A derivations, then fills password-reset expiry for NOT NULL hardening.
/// </summary>
public static class PlatformAuthHardeningBackfillApplicator
{
    public static async Task ApplyAsync(
        EPosDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        await PlatformAuthAlignmentBackfillApplicator.ApplyAsync(dbContext, cancellationToken);

        var passwordResetTokens = await dbContext.PlatformPasswordResetTokens.ToListAsync(cancellationToken);
        foreach (var token in passwordResetTokens)
        {
            if (token.ExpiresAt is null)
            {
                token.ApplyHardeningBackfill();
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
