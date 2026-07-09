using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Mirrors Phase 7A migration backfill rules for integration testing.
/// </summary>
public static class TenantUsageCounterAlignmentBackfillApplicator
{
    public sealed class AmbiguousFeatureLimitMappingException : Exception
    {
        public AmbiguousFeatureLimitMappingException(Guid platformFeatureId, int activeLimitCount)
            : base(
                $"tenant_usage_counters backfill requires exactly one active feature_limit_definitions row for platform_feature_id '{platformFeatureId}', found {activeLimitCount}.")
        {
            PlatformFeatureId = platformFeatureId;
            ActiveLimitCount = activeLimitCount;
        }

        public Guid PlatformFeatureId { get; }
        public int ActiveLimitCount { get; }
    }

    public static async Task ApplyAsync(
        EPosDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var counters = await dbContext.TenantUsageCounters.ToListAsync(cancellationToken);
        if (counters.Count == 0)
        {
            return;
        }

        var activeLimits = await dbContext.FeatureLimitDefinitions
            .AsNoTracking()
            .Where(x => x.Status == SubscriptionCatalogConstants.RecordStatus.Active)
            .ToListAsync(cancellationToken);

        var limitsByFeatureId = activeLimits
            .GroupBy(x => x.PlatformFeatureId)
            .ToDictionary(x => x.Key, x => x.ToList());

        foreach (var counter in counters)
        {
            if (counter.FeatureLimitDefinitionId != Guid.Empty)
            {
                continue;
            }

            if (!limitsByFeatureId.TryGetValue(counter.PlatformFeatureId, out var matches))
            {
                throw new AmbiguousFeatureLimitMappingException(counter.PlatformFeatureId, 0);
            }

            if (matches.Count != 1)
            {
                throw new AmbiguousFeatureLimitMappingException(counter.PlatformFeatureId, matches.Count);
            }

            var usageScope = string.IsNullOrWhiteSpace(counter.UsageScope)
                ? TenantUsageCounterAlignmentConstants.UsageScope.Tenant
                : counter.UsageScope.Trim().ToUpperInvariant();

            var status = string.IsNullOrWhiteSpace(counter.Status)
                ? SubscriptionCatalogConstants.RecordStatus.Active
                : counter.Status.Trim().ToUpperInvariant();

            var periodStart = counter.PeriodStart != default
                ? counter.PeriodStart
                : TenantUsageCounter.ParseUsagePeriodStart(counter.UsagePeriodStart, counter.CreatedAt);

            counter.ApplyAlignmentBackfill(
                matches[0].Id,
                usageScope,
                status,
                periodStart);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
