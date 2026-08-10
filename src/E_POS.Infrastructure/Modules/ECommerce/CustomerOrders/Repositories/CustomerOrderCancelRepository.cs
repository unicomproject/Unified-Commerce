using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.Shared.Media;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;

public sealed class CustomerOrderCancelRepository : CustomerOrderRepositoryBase, ICustomerOrderCancelRepository
{
    public CustomerOrderCancelRepository(EPosDbContext dbContext, IMediaReadUrlResolver? mediaReadUrlResolver = null)
        : base(dbContext, mediaReadUrlResolver)
    {
    }

    public async Task<CustomerOrderCancelRepositoryResult> CancelAsync(
        Guid tenantId,
        Guid customerId,
        Guid orderId,
        string? reason,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var order = await DbContext.SalesOrders
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.CustomerId == customerId &&
                x.Id == orderId &&
                x.OrderType == ClickAndCollectOrderType,
                cancellationToken);

        if (order is null)
            return CustomerOrderCancelRepositoryResult.Failure("customer_orders.not_found");

        var oldOrderStatus = order.Status;
        var oldFulfillmentStatus = order.FulfillmentStatus;

        try
        {
            order.CancelClickAndCollectByCustomer(reason, now);
        }
        catch (InvalidOperationException ex)
        {
            return CustomerOrderCancelRepositoryResult.Failure(
                "customer_orders.invalid_transition",
                ex.Message);
        }

        await AddStatusHistoryAsync(
            tenantId,
            order,
            oldOrderStatus,
            oldFulfillmentStatus,
            now,
            cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        var status = MapUiStatus(order);
        var response = new CustomerOrderCancelResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = status,
            StatusLabel = MapStatusLabel(status),
            CancelledAt = order.CancelledAt ?? now,
            Message = "Order cancelled successfully."
        };
        var notificationContext = order.CustomerId.HasValue
            ? new CustomerOrderNotificationContext(tenantId, order.CustomerId.Value, order.Id, order.OrderNumber)
            : null;
        return CustomerOrderCancelRepositoryResult.Success(response, notificationContext);
    }

}
