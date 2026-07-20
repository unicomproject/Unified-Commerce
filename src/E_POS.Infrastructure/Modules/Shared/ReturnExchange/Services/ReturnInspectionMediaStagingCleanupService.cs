using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace E_POS.Infrastructure.Modules.Shared.ReturnExchange.Services;

public sealed class ReturnInspectionMediaStagingCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ReturnInspectionMediaStagingCleanupService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        do { await CleanupAsync(stoppingToken); }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EPosDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IReturnInspectionMediaStorage>();
        var now = DateTimeOffset.UtcNow;

        // Expire drafts past their lifetime.
        var expiredDrafts = await db.ReturnInspectionDrafts
            .Where(x => x.Status != "CONSUMED" &&
                        x.Status != "CANCELLED" &&
                        x.ExpiresAt < now)
            .ToListAsync(cancellationToken);
        foreach (var draft in expiredDrafts)
        {
            draft.Cancel();
        }

        // Expire orphan staging media only when not attached to an active non-expired draft.
        var expiredMedia = await db.ReturnInspectionMediaStaging
            .Where(x => x.Status == "STAGED" && x.ExpiresAt < now)
            .ToListAsync(cancellationToken);
        foreach (var item in expiredMedia)
        {
            if (item.InspectionDraftId.HasValue)
            {
                var draft = await db.ReturnInspectionDrafts.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == item.InspectionDraftId.Value, cancellationToken);
                if (draft is not null && !draft.IsExpired(now) &&
                    !string.Equals(draft.Status, "CONSUMED", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            await storage.DeleteAsync(item.StorageKey, cancellationToken);
            item.MarkExpired();
        }

        if (expiredDrafts.Count > 0 || expiredMedia.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
