using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosNotificationRepository
{
    Task<NotificationInboxQueryResult> GetTenantUserInboxAsync(
        Guid tenantId,
        Guid tenantUserId,
        IReadOnlyCollection<string> allowedSourceModules,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> GetTenantUserUnreadCountAsync(
        Guid tenantId,
        Guid tenantUserId,
        IReadOnlyCollection<string> allowedSourceModules,
        CancellationToken cancellationToken);
}
