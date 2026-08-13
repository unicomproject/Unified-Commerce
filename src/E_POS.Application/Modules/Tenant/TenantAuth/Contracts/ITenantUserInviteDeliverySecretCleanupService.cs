namespace E_POS.Application.Modules.Tenant.TenantAuth.Contracts;

public interface ITenantUserInviteDeliverySecretCleanupService
{
    Task<TenantUserInviteDeliverySecretCleanupResult> CleanupBatchAsync(
        int batchSize,
        CancellationToken cancellationToken);
}

public sealed record TenantUserInviteDeliverySecretCleanupResult(
    int ClaimedCount,
    int PurgedCount);
