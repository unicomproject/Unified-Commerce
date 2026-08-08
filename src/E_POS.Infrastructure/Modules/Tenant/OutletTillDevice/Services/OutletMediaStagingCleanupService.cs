using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Services;

/// <summary>
/// Periodic background service that:
/// 1. Claims ACTIVE unlinked staged images older than 24 hours as DELETE_PENDING.
/// 2. Retries DELETE_PENDING assets whose next_retry_at has passed, using bounded exponential backoff.
/// </summary>
public sealed class OutletMediaStagingCleanupService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan OrphanAge = TimeSpan.FromHours(24);
    private static readonly TimeSpan BaseRetryDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromHours(12);
    private const int MaxRetryCount = 10;
    private const string OutletPurpose = "OUTLET_PRIMARY_IMAGE";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutletMediaStagingCleanupService> _logger;

    public OutletMediaStagingCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutletMediaStagingCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollingInterval);
        do
        {
            await RunCleanupPassAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunCleanupPassAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ClaimOrphansAsDeletePendingAsync(cancellationToken);
            await ProcessDeletePendingAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Service is stopping — exit cleanly.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during outlet media staging cleanup pass.");
        }
    }

    /// <summary>
    /// Phase 1: Claim ACTIVE unlinked outlet images older than 24 hours as DELETE_PENDING
    /// using FOR UPDATE SKIP LOCKED in a short transaction, then commit before any I/O.
    /// </summary>
    private async Task ClaimOrphansAsDeletePendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EPosDbContext>();
        var now = DateTimeOffset.UtcNow;
        var cutoff = now - OrphanAge;

        // Find ACTIVE outlet media assets that are unlinked (not referenced by any outlet row)
        // and are older than the orphan cutoff. Use FOR UPDATE SKIP LOCKED.
        var orphans = await db.MediaAssets
            .FromSqlRaw("""
                SELECT ma.* FROM media_assets ma
                WHERE ma.asset_purpose = {0}
                  AND ma.status = 'ACTIVE'
                  AND ma.created_at < {1}
                  AND NOT EXISTS (
                      SELECT 1 FROM outlets o
                      WHERE o.tenant_id = ma.tenant_id
                        AND o.primary_image_media_asset_id = ma.id
                        AND o.status != 'DELETED'
                  )
                FOR UPDATE SKIP LOCKED
                """, OutletPurpose, cutoff)
            .ToListAsync(cancellationToken);

        if (orphans.Count == 0) return;

        foreach (var asset in orphans)
        {
            asset.MarkDeletePending(null, now);
        }

        // Commit the DELETE_PENDING claim and release row locks before touching storage.
        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Claimed {Count} orphaned outlet media assets as DELETE_PENDING.", orphans.Count);
    }

    /// <summary>
    /// Phase 2: Retry DELETE_PENDING assets whose next_retry_at has passed.
    /// Delete blob outside the database transaction. On success mark DELETED;
    /// on failure record the error and schedule exponential backoff retry.
    /// </summary>
    private async Task ProcessDeletePendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EPosDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IMediaObjectStorage>();
        var now = DateTimeOffset.UtcNow;

        // Claim retryable DELETE_PENDING rows with a short-transaction row lock.
        var retryable = await db.MediaAssets
            .FromSqlRaw("""
                SELECT ma.* FROM media_assets ma
                WHERE ma.asset_purpose = {0}
                  AND ma.status = 'DELETE_PENDING'
                  AND ma.deletion_retry_count < {1}
                  AND (ma.next_retry_at IS NULL OR ma.next_retry_at <= {2})
                FOR UPDATE SKIP LOCKED
                """, OutletPurpose, MaxRetryCount, now)
            .ToListAsync(cancellationToken);

        if (retryable.Count == 0) return;

        // Commit the claim (retain DELETE_PENDING status) to release row locks.
        await db.SaveChangesAsync(cancellationToken);

        foreach (var asset in retryable)
        {
            await DeleteSingleAssetAsync(db, storage, asset, now, cancellationToken);
        }
    }

    private async Task DeleteSingleAssetAsync(
        EPosDbContext db,
        IMediaObjectStorage storage,
        MediaAsset asset,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            await storage.DeleteIfExistsAsync(asset.ContainerName, asset.StorageKey, cancellationToken);

            // Mark DELETED in a new transaction after successful blob deletion.
            using var deleteScope = _scopeFactory.CreateScope();
            var deleteDb = deleteScope.ServiceProvider.GetRequiredService<EPosDbContext>();
            var tracked = await deleteDb.MediaAssets
                .FirstOrDefaultAsync(x => x.Id == asset.Id, cancellationToken);
            if (tracked is not null)
            {
                tracked.MarkDeleted(null, now);
                await deleteDb.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Deleted outlet media asset {AssetId} (StorageKey={StorageKey}).",
                asset.Id, asset.StorageKey);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to delete outlet media asset {AssetId} (RetryCount={RetryCount}). Will retry with backoff.",
                asset.Id, asset.DeletionRetryCount);

            // Record failure and schedule exponential backoff retry.
            using var failScope = _scopeFactory.CreateScope();
            var failDb = failScope.ServiceProvider.GetRequiredService<EPosDbContext>();
            var tracked = await failDb.MediaAssets
                .FirstOrDefaultAsync(x => x.Id == asset.Id, cancellationToken);
            if (tracked is not null)
            {
                var backoff = ComputeBackoff(tracked.DeletionRetryCount);
                tracked.RecordDeletionFailure(
                    ex.Message.Length > 500 ? ex.Message[..500] : ex.Message,
                    now + backoff,
                    null,
                    now);
                await failDb.SaveChangesAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Bounded exponential backoff: 2^retryCount * 5 minutes, capped at 12 hours.
    /// </summary>
    private static TimeSpan ComputeBackoff(int retryCount)
    {
        var minutes = Math.Pow(2, retryCount) * BaseRetryDelay.TotalMinutes;
        return TimeSpan.FromMinutes(Math.Min(minutes, MaxRetryDelay.TotalMinutes));
    }
}
