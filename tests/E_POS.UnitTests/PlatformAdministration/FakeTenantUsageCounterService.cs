using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Entities;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class FakeTenantUsageCounterService : ITenantUsageCounterService
{
    public bool ValidateCalled { get; private set; }
    public bool SeedCalled { get; private set; }
    public Exception? ValidateException { get; init; }
    public Exception? SeedException { get; init; }
    public TenantCapacityCounterSeedRequest? LastSeedRequest { get; private set; }

    public Task<TenantUsageCounter?> GetByKeyAsync(
        TenantUsageCounterKey key,
        CancellationToken cancellationToken) =>
        Task.FromResult<TenantUsageCounter?>(null);

    public Task<TenantUsageCounter> UpsertAsync(
        TenantUsageCounterUpsertRequest request,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<TenantUsageCounter> SetCurrentValueAsync(
        TenantUsageCounterKey key,
        decimal currentValue,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<TenantUsageCounter> IncrementCurrentValueAsync(
        TenantUsageCounterKey key,
        decimal incrementBy,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task ValidateCanonicalCapacityLimitDefinitionsAsync(CancellationToken cancellationToken)
    {
        ValidateCalled = true;
        if (ValidateException is not null)
        {
            throw ValidateException;
        }

        return Task.CompletedTask;
    }

    public Task SeedTenantCapacityCountersAsync(
        Guid tenantId,
        DateTimeOffset periodStart,
        DateTimeOffset? periodEnd,
        int? maxOutlets,
        int? maxUsers,
        int? maxTills,
        CancellationToken cancellationToken)
    {
        SeedCalled = true;
        LastSeedRequest = new TenantCapacityCounterSeedRequest(
            tenantId,
            periodStart,
            periodEnd,
            maxOutlets,
            maxUsers,
            maxTills);

        if (SeedException is not null)
        {
            throw SeedException;
        }

        return Task.CompletedTask;
    }
}

public sealed record TenantCapacityCounterSeedRequest(
    Guid TenantId,
    DateTimeOffset PeriodStart,
    DateTimeOffset? PeriodEnd,
    int? MaxOutlets,
    int? MaxUsers,
    int? MaxTills);
