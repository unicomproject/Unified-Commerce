namespace E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;

public sealed record CustomerOrderNotificationContext(
    Guid TenantId,
    Guid CustomerId,
    Guid OrderId,
    string OrderNumber);