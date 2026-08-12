using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.TenantAuth.Services;

public sealed class TenantUserInviteDeliverySecretCleanupService : ITenantUserInviteDeliverySecretCleanupService
{
    private const int DefaultBatchSize = 100;
    private const int MaxBatchSize = 100;

    private readonly EPosDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<TenantUserInviteDeliverySecretCleanupService> _logger;

    public TenantUserInviteDeliverySecretCleanupService(
        EPosDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        ILogger<TenantUserInviteDeliverySecretCleanupService> logger)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<TenantUserInviteDeliverySecretCleanupResult> CleanupBatchAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        var safeBatchSize = Math.Clamp(batchSize <= 0 ? DefaultBatchSize : batchSize, 1, MaxBatchSize);
        var now = _dateTimeProvider.UtcNow;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var secrets = _dbContext.Database.IsNpgsql()
            ? await ClaimPostgreSqlBatchAsync(now, safeBatchSize, cancellationToken)
            : await ClaimPortableBatchAsync(now, safeBatchSize, cancellationToken);

        foreach (var secret in secrets.Where(secret => secret.PurgedAt is null))
        {
            secret.Purge(now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (secrets.Count > 0)
        {
            _logger.LogInformation(
                "Purged {Count} tenant user invitation delivery secrets.",
                secrets.Count);
        }

        return new TenantUserInviteDeliverySecretCleanupResult(secrets.Count, secrets.Count);
    }

    private Task<List<TenantUserInviteDeliverySecret>> ClaimPostgreSqlBatchAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        return _dbContext.TenantUserInviteDeliverySecrets
            .FromSqlRaw(
                """
                SELECT s.* FROM tenant_user_invite_delivery_secrets s
                LEFT JOIN user_invites i ON i.id = s.invite_id
                WHERE s.purged_at IS NULL
                  AND (
                    s.expires_at <= {0}
                    OR i.id IS NULL
                    OR i.accepted_at IS NOT NULL
                    OR i.cancelled_at IS NOT NULL
                    OR i.invite_status IN ({1}, {2}, {3}, {4})
                    OR EXISTS (
                        SELECT 1 FROM integration_outbox_messages m
                        WHERE m.deduplication_key = ('tenant.user_invited:' || replace(s.invite_id::text, '-', ''))
                          AND m.status = 'FAILED_FINAL'
                    )
                  )
                ORDER BY s.expires_at ASC, s.created_at ASC, s.id ASC
                LIMIT {5}
                FOR UPDATE OF s SKIP LOCKED
                """,
                now,
                UserInviteConstants.StatusAccepted,
                UserInviteConstants.StatusRevoked,
                UserInviteConstants.StatusCancelled,
                UserInviteConstants.StatusExpired,
                batchSize)
            .ToListAsync(cancellationToken);
    }

    private Task<List<TenantUserInviteDeliverySecret>> ClaimPortableBatchAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        return (
                from secret in _dbContext.TenantUserInviteDeliverySecrets
                join invite in _dbContext.UserInvites on secret.InviteId equals invite.Id into inviteJoin
                from invite in inviteJoin.DefaultIfEmpty()
                where secret.PurgedAt == null &&
                      (secret.ExpiresAt <= now ||
                       invite == null ||
                       invite.AcceptedAt != null ||
                       invite.CancelledAt != null ||
                       invite.InviteStatus == UserInviteConstants.StatusAccepted ||
                       invite.InviteStatus == UserInviteConstants.StatusRevoked ||
                       invite.InviteStatus == UserInviteConstants.StatusCancelled ||
                       invite.InviteStatus == UserInviteConstants.StatusExpired)
                orderby secret.ExpiresAt, secret.CreatedAt, secret.Id
                select secret)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
