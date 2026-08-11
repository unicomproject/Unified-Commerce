using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Services;

/// <summary>
/// Claims STAGED unlinked product images older than 24 hours as DELETE_PENDING,
/// then deletes blobs with bounded exponential backoff retries.
/// </summary>
public sealed class ProductMediaStagingCleanupService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan OrphanAge = TimeSpan.FromHours(24);
    private static readonly TimeSpan BaseRetryDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromHours(12);
    private const int MaxRetryCount = 10;
    private const string ProductPurpose = ProductConstants.ProductImagePurpose;
    private const string StagedStatus = "STAGED";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductMediaStagingCleanupService> _logger;

    public ProductMediaStagingCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductMediaStagingCleanupService> logger)
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during product media staging cleanup pass.");
        }
    }

    private async Task ClaimOrphansAsDeletePendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EPosDbContext>();
        var now = DateTimeOffset.UtcNow;
        var cutoff = now - OrphanAge;

        var orphans = await db.MediaAssets
            .FromSqlRaw("""
                SELECT ma.* FROM media_assets ma
                WHERE ma.asset_purpose = {0}
                  AND ma.status = {1}
                  AND ma.created_at < {2}
                  AND NOT EXISTS (
                      SELECT 1 FROM product_images pi
                      WHERE pi.tenant_id = ma.tenant_id
                        AND pi.media_asset_id = ma.id
                        AND pi.status != 'DELETED'
                  )
                FOR UPDATE SKIP LOCKED
                """, ProductPurpose, StagedStatus, cutoff)
            .ToListAsync(cancellationToken);

        if (orphans.Count == 0)
        {
            return;
        }

        foreach (var asset in orphans)
        {
            asset.MarkDeletePending(null, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Claimed {Count} orphaned staged product media assets as DELETE_PENDING.", orphans.Count);
    }

    private async Task ProcessDeletePendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EPosDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IMediaObjectStorage>();
        var now = DateTimeOffset.UtcNow;

        var retryable = await db.MediaAssets
            .FromSqlRaw("""
                SELECT ma.* FROM media_assets ma
                WHERE ma.asset_purpose = {0}
                  AND ma.status = 'DELETE_PENDING'
                  AND ma.deletion_retry_count < {1}
                  AND (ma.next_retry_at IS NULL OR ma.next_retry_at <= {2})
                FOR UPDATE SKIP LOCKED
                """, ProductPurpose, MaxRetryCount, now)
            .ToListAsync(cancellationToken);

        if (retryable.Count == 0)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var asset in retryable)
        {
            await DeleteSingleAssetAsync(storage, asset, now, cancellationToken);
        }
    }

    private async Task DeleteSingleAssetAsync(
        IMediaObjectStorage storage,
        MediaAsset asset,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            await storage.DeleteIfExistsAsync(asset.ContainerName, asset.StorageKey, cancellationToken);

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
                "Deleted staged product media asset {AssetId} (StorageKey={StorageKey}).",
                asset.Id, asset.StorageKey);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to delete staged product media asset {AssetId} (RetryCount={RetryCount}). Will retry with backoff.",
                asset.Id, asset.DeletionRetryCount);

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

    private static TimeSpan ComputeBackoff(int retryCount)
    {
        var minutes = Math.Pow(2, retryCount) * BaseRetryDelay.TotalMinutes;
        return TimeSpan.FromMinutes(Math.Min(minutes, MaxRetryDelay.TotalMinutes));
    }
}
