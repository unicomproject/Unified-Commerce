using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Notification.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;

public sealed partial class PosOnlineOrderPickingRepository
{
    public async Task<ApplicationResult<NotificationCreateResult>> ExecuteNotificationAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId,
        Func<SalesOrder, FulfillmentOrder, PickupOrder?, CancellationToken,
            Task<ApplicationResult<NotificationCreateResult>>> notify,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateAccessAsync(tenantId, tenantUserId, outletId, cancellationToken);
        if (accessError is not null)
            return NotificationFailure(accessError, "Access to this outlet is unavailable.");

        await using var transaction = DbContext.Database.IsRelational()
            ? await DbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        // Keep only this operation's tracked entries eligible for cleanup after rollback.
        var originalEntries = DbContext.ChangeTracker.Entries().Select(x => x.Entity).ToHashSet();
        try
        {
            var aggregate = await LoadAggregateAsync(tenantId, outletId, orderId, false, cancellationToken);
            if (aggregate is null)
                return NotificationFailure("online_orders.not_found", "Online order was not found.");

            if (DbContext.Database.IsNpgsql())
            {
                // Database row locks serialize independent cashiers/processes, without changing row_version.
                await DbContext.SalesOrders.FromSqlInterpolated(
                    $"SELECT * FROM sales_orders WHERE tenant_id = {tenantId} AND id = {orderId} FOR UPDATE")
                    .AsNoTracking().ToListAsync(cancellationToken);
                await DbContext.FulfillmentOrders.FromSqlInterpolated(
                    $"SELECT * FROM fulfillment_orders WHERE tenant_id = {tenantId} AND id = {aggregate.Fulfillment.Id} FOR UPDATE")
                    .AsNoTracking().ToListAsync(cancellationToken);
                await DbContext.PickupOrders.FromSqlInterpolated(
                    $"SELECT * FROM pickup_orders WHERE tenant_id = {tenantId} AND fulfillment_order_id = {aggregate.Fulfillment.Id} FOR UPDATE")
                    .AsNoTracking().ToListAsync(cancellationToken);
                aggregate = await LoadAggregateAsync(tenantId, outletId, orderId, false, cancellationToken);
                if (aggregate is null)
                    return NotificationFailure("online_orders.not_found", "Online order was not found.");
            }

            var pickup = await DbContext.PickupOrders.AsNoTracking().SingleOrDefaultAsync(
                x => x.TenantId == tenantId && x.FulfillmentOrderId == aggregate.Fulfillment.Id,
                cancellationToken);
            if (aggregate.Order.CustomerId is not { } customerId ||
                !await DbContext.Customers.AsNoTracking().AnyAsync(
                    x => x.TenantId == tenantId && x.Id == customerId && x.Status == "ACTIVE", cancellationToken))
                return NotificationFailure("online_orders.notification_recipient_unavailable",
                    "A customer notification destination is unavailable.");

            var result = await notify(aggregate.Order, aggregate.Fulfillment, pickup, cancellationToken);
            if (!result.IsSuccess)
            {
                if (transaction is not null)
                    await transaction.RollbackAsync(cancellationToken);
                DetachNotificationEntries(originalEntries);
                return result;
            }

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            DetachNotificationEntries(originalEntries);
            var postgres = ex as PostgresException ?? ex.InnerException as PostgresException;
            return postgres?.SqlState is PostgresErrorCodes.UniqueViolation or
                PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected
                ? NotificationFailure("online_orders.concurrency_conflict", "The notification changed. Retry to retrieve its result.")
                : NotificationFailure("online_orders.notification_failed", "Customer notification could not be completed. Retry safely.");
        }
    }

    private void DetachNotificationEntries(HashSet<object> originalEntries)
    {
        foreach (var entry in DbContext.ChangeTracker.Entries().Where(x => !originalEntries.Contains(x.Entity)).ToList())
            entry.State = EntityState.Detached;
    }

    private static ApplicationResult<NotificationCreateResult> NotificationFailure(string code, string message) =>
        ApplicationResult<NotificationCreateResult>.Failure(new(code, message));
}
