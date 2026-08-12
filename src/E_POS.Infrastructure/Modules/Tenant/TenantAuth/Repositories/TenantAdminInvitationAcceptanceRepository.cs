using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.TenantAuth.Repositories;

public sealed class TenantAdminInvitationAcceptanceRepository : ITenantAdminInvitationAcceptanceRepository
{
    private readonly EPosDbContext _db;

    public TenantAdminInvitationAcceptanceRepository(EPosDbContext db) => _db = db;

    public async Task<TenantAdminInvitationAcceptanceSnapshot?> GetByTokenHashForReadAsync(
        string inviteTokenHash,
        CancellationToken cancellationToken)
    {
        var invite = await _db.UserInvites.AsNoTracking()
            .SingleOrDefaultAsync(x => x.InviteTokenHash == inviteTokenHash, cancellationToken);
        if (invite is null) return null;

        var tenant = await _db.Tenants.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == invite.TenantId, cancellationToken);
        if (tenant is null) return null;

        var user = invite.TenantUserId is null ? null : await _db.TenantUsers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == invite.TenantId && x.Id == invite.TenantUserId, cancellationToken);

        return new TenantAdminInvitationAcceptanceSnapshot
        {
            InviteId = invite.Id,
            TenantId = invite.TenantId,
            InviteStatus = invite.InviteStatus,
            ExpiresAt = invite.ExpiresAt,
            AcceptedAt = invite.AcceptedAt,
            CancelledAt = invite.CancelledAt,
            InvitedEmail = invite.InvitedEmail,
            NormalizedInvitedEmail = invite.NormalizedInvitedEmail,
            TenantStatus = tenant.Status,
            TenantDisplayName = tenant.DisplayName,
            TenantUserId = user?.Id,
            TenantUserStatus = user?.AccountStatus
        };
    }

    public async Task<TResult> ExecuteClaimAsync<TResult>(
        string inviteTokenHash,
        Func<TenantAdminInvitationAcceptanceClaim?, CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var invite = await _db.UserInvites
            .FromSqlInterpolated($@"SELECT * FROM user_invites WHERE invite_token_hash = {inviteTokenHash} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (invite is null)
        {
            var missing = await action(null, cancellationToken);
            await tx.RollbackAsync(cancellationToken);
            return missing;
        }

        var tenant = await _db.Tenants
            .SingleOrDefaultAsync(x => x.Id == invite.TenantId, cancellationToken);
        var user = tenant is null || invite.TenantUserId is null
            ? null
            : await _db.TenantUsers
                .SingleOrDefaultAsync(x => x.TenantId == invite.TenantId && x.Id == invite.TenantUserId, cancellationToken);

        if (tenant is null || user is null)
        {
            var missing = await action(null, cancellationToken);
            await tx.RollbackAsync(cancellationToken);
            return missing;
        }

        var siblings = await _db.UserInvites
            .Where(x => x.TenantId == invite.TenantId &&
                        x.TenantUserId == invite.TenantUserId &&
                        (x.InviteStatus == UserInviteConstants.StatusPending ||
                         x.InviteStatus == UserInviteConstants.StatusSent))
            .ToListAsync(cancellationToken);

        var operation = await _db.PlatformTenantOnboardingOperations
            .Where(x => x.TenantId == invite.TenantId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var claim = new TenantAdminInvitationAcceptanceClaim
        {
            Invite = invite,
            User = user,
            Tenant = tenant,
            Operation = operation,
            SiblingOpenInvites = siblings
        };

        try
        {
            var result = await action(claim, cancellationToken);
            if (result is Application.Common.Models.ApplicationResult { IsFailure: true })
            {
                _db.ChangeTracker.Clear();
                await tx.RollbackAsync(cancellationToken);
                return result;
            }

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            _db.ChangeTracker.Clear();
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
