using E_POS.Domain.Modules.Platform.Subscription.Entities;

namespace E_POS.Application.Modules.Platform.Subscription.Contracts;

public interface ITenantUsageCounterRepository
{
    Task<TenantUsageCounter?> GetByKeyAsync(
        TenantUsageCounterKey key,
        CancellationToken cancellationToken);

    Task<TenantUsageCounter> UpsertAsync(
        TenantUsageCounterUpsertRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<TenantUsageCounter> SetCurrentValueAsync(
        TenantUsageCounterKey key,
        decimal currentValue,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<TenantUsageCounter> IncrementCurrentValueAsync(
        TenantUsageCounterKey key,
        decimal incrementBy,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<CanonicalCapacityLimitDefinition>> GetActiveCanonicalCapacityLimitDefinitionsAsync(
        CancellationToken cancellationToken);
}
