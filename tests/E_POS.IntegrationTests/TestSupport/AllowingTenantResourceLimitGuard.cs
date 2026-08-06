using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.IntegrationTests.TestSupport;

internal sealed class AllowingTenantResourceLimitGuard : ITenantResourceLimitGuard
{
    public Task<TenantResourceLimitEvaluation> EvaluateAsync(
        Guid tenantId,
        string limitKey,
        int requestedIncrease,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new TenantResourceLimitEvaluation(
            limitKey,
            TenantSubscriptionLimitKeys.TryGet(limitKey, out var definition) ? definition.Resource : "unknown",
            0,
            requestedIncrease,
            null,
            null,
            true,
            true,
            false,
            null,
            null));
    }

    public async Task<TenantResourceLimitGuardResult<T>> ExecuteWithinCapacityAsync<T>(
        Guid tenantId,
        string limitKey,
        int requestedIncrease,
        Func<CancellationToken, Task<TenantResourceCapacityOperationResult<T>>> operation,
        CancellationToken cancellationToken)
    {
        var outcome = await operation(cancellationToken);
        var evaluation = await EvaluateAsync(tenantId, limitKey, requestedIncrease, cancellationToken);
        return TenantResourceLimitGuardResult<T>.Succeeded(outcome.Value, evaluation);
    }

    public Task<TenantResourceCapacitySnapshot> GetCapacitySnapshotAsync(
        Guid tenantId,
        string limitKey,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new TenantResourceCapacitySnapshot(
            limitKey,
            TenantSubscriptionLimitKeys.TryGet(limitKey, out var definition) ? definition.Resource : "unknown",
            0,
            null,
            null,
            true,
            true,
            false));
    }
}
