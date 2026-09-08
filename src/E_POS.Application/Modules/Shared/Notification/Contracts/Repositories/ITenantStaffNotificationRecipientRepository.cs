namespace E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;

public interface ITenantStaffNotificationRecipientRepository
{
    Task<IReadOnlyList<Guid>> GetActiveStaffTenantUserIdsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);
}
