using E_POS.Domain.Modules.Platform.Subscription.Entities;

namespace E_POS.Application.Modules.Platform.Subscription.Contracts;

public interface ITenantUsageCounterService
{
    Task<TenantUsageCounter?> GetByKeyAsync(
        TenantUsageCounterKey key,
        CancellationToken cancellationToken);

    Task<TenantUsageCounter> UpsertAsync(
        TenantUsageCounterUpsertRequest request,
        CancellationToken cancellationToken);

    Task<TenantUsageCounter> SetCurrentValueAsync(
        TenantUsageCounterKey key,
        decimal currentValue,
        CancellationToken cancellationToken);

    Task<TenantUsageCounter> IncrementCurrentValueAsync(
        TenantUsageCounterKey key,
        decimal incrementBy,
        CancellationToken cancellationToken);

    Task ValidateCanonicalCapacityLimitDefinitionsAsync(CancellationToken cancellationToken);

    Task SeedTenantCapacityCountersAsync(
        Guid tenantId,
        DateTimeOffset periodStart,
        DateTimeOffset? periodEnd,
        int? maxOutlets,
        int? maxUsers,
        int? maxTills,
        CancellationToken cancellationToken);
}
