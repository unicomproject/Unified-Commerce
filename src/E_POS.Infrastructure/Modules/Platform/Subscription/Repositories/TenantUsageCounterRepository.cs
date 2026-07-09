using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Repositories;

public sealed class TenantUsageCounterRepository : ITenantUsageCounterRepository
{
    private readonly EPosDbContext _dbContext;

    public TenantUsageCounterRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TenantUsageCounter?> GetByKeyAsync(
        TenantUsageCounterKey key,
        CancellationToken cancellationToken)
    {
        return FindByKeyQuery(key).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TenantUsageCounter> UpsertAsync(
        TenantUsageCounterUpsertRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var limitDefinition = await ResolveActiveFeatureLimitDefinitionAsync(
            request.FeatureLimitDefinitionId,
            cancellationToken);

        var key = new TenantUsageCounterKey(
            request.TenantId,
            request.FeatureLimitDefinitionId,
            request.UsageScope,
            request.ScopeReferenceId,
            request.PeriodStart);

        var existing = await FindByKeyQuery(key).FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            existing.UpdateFromUpsert(
                limitDefinition.PlatformFeatureId,
                request.PeriodStart,
                request.PeriodEnd,
                request.LimitValue,
                now);
            existing.SetCurrentValue(request.CurrentValue, now);
            return existing;
        }

        var counter = TenantUsageCounter.Create(
            Guid.NewGuid(),
            request.TenantId,
            limitDefinition.Id,
            limitDefinition.PlatformFeatureId,
            key.NormalizedUsageScope,
            request.ScopeReferenceId,
            request.CurrentValue,
            request.LimitValue,
            request.PeriodStart,
            request.PeriodEnd,
            now);

        await _dbContext.TenantUsageCounters.AddAsync(counter, cancellationToken);
        return counter;
    }

    public async Task<TenantUsageCounter> SetCurrentValueAsync(
        TenantUsageCounterKey key,
        decimal currentValue,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var counter = await GetRequiredByKeyAsync(key, cancellationToken);
        counter.SetCurrentValue(currentValue, now);
        return counter;
    }

    public async Task<TenantUsageCounter> IncrementCurrentValueAsync(
        TenantUsageCounterKey key,
        decimal incrementBy,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var counter = await GetRequiredByKeyAsync(key, cancellationToken);
        counter.IncrementCurrentValue(incrementBy, now);
        return counter;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CanonicalCapacityLimitDefinition>> GetActiveCanonicalCapacityLimitDefinitionsAsync(
        CancellationToken cancellationToken)
    {
        var expectedDefinitions = new (string LimitKey, Guid DefinitionId)[]
        {
            (SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitKey, SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId),
            (SubscriptionCatalogLimitSeedConstants.MaxUsersLimitKey, SubscriptionCatalogLimitSeedConstants.MaxUsersLimitDefinitionId),
            (SubscriptionCatalogLimitSeedConstants.MaxTillsLimitKey, SubscriptionCatalogLimitSeedConstants.MaxTillsLimitDefinitionId)
        };

        var expectedIds = expectedDefinitions.Select(definition => definition.DefinitionId).ToArray();
        var activeDefinitions = await _dbContext.FeatureLimitDefinitions
            .AsNoTracking()
            .Where(definition =>
                expectedIds.Contains(definition.Id) &&
                definition.Status == SubscriptionCatalogConstants.RecordStatus.Active)
            .Select(definition => new CanonicalCapacityLimitDefinition(
                definition.Id,
                definition.LimitKey,
                definition.PlatformFeatureId))
            .ToListAsync(cancellationToken);

        var activeById = activeDefinitions.ToDictionary(definition => definition.Id);

        var resolved = new List<CanonicalCapacityLimitDefinition>(expectedDefinitions.Length);
        foreach (var expected in expectedDefinitions)
        {
            if (!activeById.TryGetValue(expected.DefinitionId, out var definition))
            {
                throw new MissingCanonicalCapacityLimitDefinitionException(
                    expected.LimitKey,
                    expected.DefinitionId);
            }

            resolved.Add(definition);
        }

        return resolved;
    }

    private IQueryable<TenantUsageCounter> FindByKeyQuery(TenantUsageCounterKey key)
    {
        var normalizedScope = key.NormalizedUsageScope;

        return _dbContext.TenantUsageCounters.Where(counter =>
            counter.TenantId == key.TenantId &&
            counter.FeatureLimitDefinitionId == key.FeatureLimitDefinitionId &&
            counter.UsageScope == normalizedScope &&
            counter.ScopeReferenceId == key.ScopeReferenceId &&
            counter.PeriodStart == key.PeriodStart);
    }

    private async Task<TenantUsageCounter> GetRequiredByKeyAsync(
        TenantUsageCounterKey key,
        CancellationToken cancellationToken)
    {
        var counter = await GetByKeyAsync(key, cancellationToken);
        if (counter is null)
        {
            throw new KeyNotFoundException(
                $"Tenant usage counter was not found for tenant '{key.TenantId}', feature limit '{key.FeatureLimitDefinitionId}', scope '{key.NormalizedUsageScope}', reference '{key.ScopeReferenceId}', period '{key.PeriodStart:O}'.");
        }

        return counter;
    }

    private async Task<FeatureLimitDefinition> ResolveActiveFeatureLimitDefinitionAsync(
        Guid featureLimitDefinitionId,
        CancellationToken cancellationToken)
    {
        var limitDefinition = await _dbContext.FeatureLimitDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == featureLimitDefinitionId &&
                     x.Status == SubscriptionCatalogConstants.RecordStatus.Active,
                cancellationToken);

        if (limitDefinition is null)
        {
            throw new InactiveFeatureLimitDefinitionException(featureLimitDefinitionId);
        }

        return limitDefinition;
    }
}
