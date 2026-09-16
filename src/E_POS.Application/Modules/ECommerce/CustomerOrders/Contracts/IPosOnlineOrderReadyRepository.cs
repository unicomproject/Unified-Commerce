using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;

namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;

public interface IPosOnlineOrderReadyRepository
{
    // Holds the scoped aggregate stable until the application callback commits its notification.
    Task<ApplicationResult<NotificationCreateResult>> ExecuteNotificationAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId,
        Func<SalesOrder, FulfillmentOrder, PickupOrder?, CancellationToken,
            Task<ApplicationResult<NotificationCreateResult>>> notify,
        CancellationToken cancellationToken);
}
