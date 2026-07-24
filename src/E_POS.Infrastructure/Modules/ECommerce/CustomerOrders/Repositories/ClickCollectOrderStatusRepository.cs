using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;

public sealed class ClickCollectOrderStatusRepository : IClickCollectOrderStatusRepository
{
    private const string ClickAndCollectOrderType = "CLICK_AND_COLLECT";

    private readonly EPosDbContext _dbContext;

    public ClickCollectOrderStatusRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ClickCollectOrderStatusUpdateRepositoryResult> UpdateStatusAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid orderId,
        string targetStatus,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.SalesOrders
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Id == orderId &&
                x.OrderType == ClickAndCollectOrderType,
                cancellationToken);

        if (order is null)
            return ClickCollectOrderStatusUpdateRepositoryResult.Failure("click_collect_orders.not_found");

        var oldOrderStatus = order.Status;
        var oldFulfillmentStatus = order.FulfillmentStatus;

        try
        {
            order.UpdateClickAndCollectStatus(targetStatus, tenantUserId, now);
        }
        catch (InvalidOperationException ex)
        {
            return ClickCollectOrderStatusUpdateRepositoryResult.Failure(
                "click_collect_orders.invalid_transition",
                ex.Message);
        }

        await AddStatusHistoryAsync(
            tenantId,
            tenantUserId,
            order,
            oldOrderStatus,
            oldFulfillmentStatus,
            now,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var status = order.GetClickAndCollectCustomerStatus();
        return ClickCollectOrderStatusUpdateRepositoryResult.Success(new ClickCollectOrderStatusUpdateResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = status,
            StatusLabel = MapStatusLabel(status),
            FulfillmentStatus = order.FulfillmentStatus,
            UpdatedAt = order.UpdatedAt ?? now,
            CollectionQrAvailable = CanShowCollectionQr(status)
        });
    }

    private static string MapStatusLabel(string status) => status switch
    {
        "PENDING_CONFIRMATION" => "Pending Confirmation",
        "ACCEPTED" => "Accepted",
        "PREPARING" => "Preparing",
        "READY_FOR_COLLECTION" => "Ready for Collection",
        "COMPLETED" => "Completed",
        "CANCELLED" => "Cancelled",
        _ => status
    };

    private static bool CanShowCollectionQr(string status) =>
        status is "ACCEPTED" or "PREPARING" or "READY_FOR_COLLECTION" or "COMPLETED";

    private async Task AddStatusHistoryAsync(
        Guid tenantId,
        Guid tenantUserId,
        SalesOrder order,
        string oldOrderStatus,
        string oldFulfillmentStatus,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sequence = await _dbContext.SalesOrderStatusHistory
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == order.Id)
            .MaxAsync(x => (int?)x.SequenceNumber, cancellationToken) ?? 0;

        if (!Is(oldOrderStatus, order.Status))
        {
            _dbContext.SalesOrderStatusHistory.Add(SalesOrderStatusHistory.Create(
                Guid.NewGuid(),
                tenantId,
                order.Id,
                ++sequence,
                "ORDER_STATUS",
                oldOrderStatus,
                order.Status,
                tenantUserId,
                now,
                "Click & collect status update"));
        }

        if (!Is(oldFulfillmentStatus, order.FulfillmentStatus))
        {
            _dbContext.SalesOrderStatusHistory.Add(SalesOrderStatusHistory.Create(
                Guid.NewGuid(),
                tenantId,
                order.Id,
                ++sequence,
                "FULFILLMENT_STATUS",
                oldFulfillmentStatus,
                order.FulfillmentStatus,
                tenantUserId,
                now,
                "Click & collect status update"));
        }
    }

    private static bool Is(string value, string expected) =>
        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
}
