using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IPosOnlineOrderReadyService
{
    Task<ApplicationResult<NotificationCreateResult>> NotifyAsync(
        TenantRequestContext context, Guid outletId, Guid orderId, CancellationToken cancellationToken);
}
