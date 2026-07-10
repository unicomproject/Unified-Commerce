using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;

namespace E_POS.Application.Modules.Platform.Subscription.Services;

public sealed class TenantUsageCounterService : ITenantUsageCounterService
{
    private readonly ITenantUsageCounterRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TenantUsageCounterService(
        ITenantUsageCounterRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<TenantUsageCounter?> GetByKeyAsync(
        TenantUsageCounterKey key,
        CancellationToken cancellationToken)
    {
        return _repository.GetByKeyAsync(key, cancellationToken);
    }

    public async Task<TenantUsageCounter> UpsertAsync(
        TenantUsageCounterUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var counter = await _repository.UpsertAsync(
            request,
            _dateTimeProvider.UtcNow,
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return counter;
    }

    public async Task<TenantUsageCounter> SetCurrentValueAsync(
        TenantUsageCounterKey key,
        decimal currentValue,
        CancellationToken cancellationToken)
    {
        var counter = await _repository.SetCurrentValueAsync(
            key,
            currentValue,
            _dateTimeProvider.UtcNow,
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return counter;
    }

    public async Task<TenantUsageCounter> IncrementCurrentValueAsync(
        TenantUsageCounterKey key,
        decimal incrementBy,
        CancellationToken cancellationToken)
    {
        var counter = await _repository.IncrementCurrentValueAsync(
            key,
            incrementBy,
            _dateTimeProvider.UtcNow,
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return counter;
    }

    public Task ValidateCanonicalCapacityLimitDefinitionsAsync(CancellationToken cancellationToken)
    {
        return ValidateCanonicalCapacityLimitDefinitionsInternalAsync(cancellationToken);
    }

    public async Task SeedTenantCapacityCountersAsync(
        Guid tenantId,
        DateTimeOffset periodStart,
        DateTimeOffset? periodEnd,
        int? maxOutlets,
        int? maxUsers,
        int? maxTills,
        CancellationToken cancellationToken)
    {
        var definitions = await _repository.GetActiveCanonicalCapacityLimitDefinitionsAsync(cancellationToken);
        var limitsByKey = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase)
        {
            [SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitKey] = maxOutlets,
            [SubscriptionCatalogLimitSeedConstants.MaxUsersLimitKey] = maxUsers,
            [SubscriptionCatalogLimitSeedConstants.MaxTillsLimitKey] = maxTills
        };

        var now = _dateTimeProvider.UtcNow;

        foreach (var definition in definitions)
        {
            if (!limitsByKey.TryGetValue(definition.LimitKey, out var effectiveLimit))
            {
                throw new MissingCanonicalCapacityLimitDefinitionException(
                    definition.LimitKey,
                    definition.Id);
            }

            var limitValue = effectiveLimit.HasValue ? (decimal?)effectiveLimit.Value : null;

            await _repository.UpsertAsync(
                new TenantUsageCounterUpsertRequest(
                    tenantId,
                    definition.Id,
                    TenantUsageCounterAlignmentConstants.UsageScope.Tenant,
                    null,
                    periodStart,
                    periodEnd,
                    CurrentValue: 0m,
                    LimitValue: limitValue),
                now,
                cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateCanonicalCapacityLimitDefinitionsInternalAsync(CancellationToken cancellationToken)
    {
        _ = await _repository.GetActiveCanonicalCapacityLimitDefinitionsAsync(cancellationToken);
    }
}
