using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;

public sealed partial class PosOnlineOrderPickingRepository
{
    private async Task<PosOnlineOrderPickingRepositoryResult> ExecuteMutationAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId, Guid lineId,
        long expectedVersion, DateTimeOffset now, CancellationToken cancellationToken,
        Func<PickingAggregate, FulfillmentOrderLine,
            E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrderLine, int,
            Task<MutationResult>> mutate)
    {
        IDbContextTransaction? transaction = null;
        if (DbContext.Database.IsRelational())
            transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var accessError = await ValidateAccessAsync(tenantId, tenantUserId, outletId, cancellationToken);
            if (accessError is not null)
                return await RollbackFailureAsync(transaction, accessError, cancellationToken);

            var aggregate = await LoadAggregateAsync(tenantId, outletId, orderId, tracked: true, cancellationToken);
            if (aggregate is null)
                return await RollbackFailureAsync(transaction, "online_orders.not_found", cancellationToken);
            if (aggregate.Fulfillment.FulfillmentStatus != "PICKING")
                return await RollbackFailureAsync(transaction, "online_orders.invalid_state", cancellationToken);
            if (expectedVersion <= 0 || aggregate.Fulfillment.RowVersion != expectedVersion)
                return await RollbackFailureAsync(transaction, "online_orders.concurrency_conflict", cancellationToken);

            var lineRow = await (
                from fulfillmentLine in DbContext.FulfillmentOrderLines
                join salesLine in DbContext.SalesOrderLines
                    on new { fulfillmentLine.TenantId, Id = fulfillmentLine.SalesOrderLineId }
                    equals new { salesLine.TenantId, Id = salesLine.Id }
                where fulfillmentLine.TenantId == tenantId && fulfillmentLine.Id == lineId &&
                      fulfillmentLine.FulfillmentOrderId == aggregate.Fulfillment.Id &&
                      salesLine.SalesOrderId == orderId && salesLine.LineStatus != "CANCELLED"
                select new { FulfillmentLine = fulfillmentLine, SalesLine = salesLine })
                .FirstOrDefaultAsync(cancellationToken);
            if (lineRow is null)
                return await RollbackFailureAsync(transaction, "online_orders.invalid_line", cancellationToken);

            var sequence = await DbContext.FulfillmentOrderEvents
                .Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == aggregate.Fulfillment.Id)
                .MaxAsync(x => (int?)x.SequenceNumber, cancellationToken) ?? 0;
            var mutation = await mutate(aggregate, lineRow.FulfillmentLine, lineRow.SalesLine, sequence);
            if (!mutation.IsSuccess)
                return await RollbackFailureAsync(transaction, mutation.ErrorCode!, cancellationToken);

            await DbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            var totalLines = await DbContext.FulfillmentOrderLines.AsNoTracking()
                .CountAsync(x => x.TenantId == tenantId &&
                                 x.FulfillmentOrderId == aggregate.Fulfillment.Id, cancellationToken);
            var completedLines = await DbContext.FulfillmentOrderLines.AsNoTracking()
                .CountAsync(x => x.TenantId == tenantId &&
                                 x.FulfillmentOrderId == aggregate.Fulfillment.Id &&
                                 x.PickedQuantity + x.CancelledQuantity >= x.RequestedQuantity,
                    cancellationToken);
            return PosOnlineOrderPickingRepositoryResult.CommandSuccess(
                new PosOnlineOrderPickingCommandResponse
                {
                    OrderId = aggregate.Order.Id,
                    FulfillmentOrderId = aggregate.Fulfillment.Id,
                    Status = aggregate.Fulfillment.FulfillmentStatus,
                    TotalLines = totalLines,
                    CompletedLines = completedLines,
                    CanPack = totalLines > 0 && completedLines == totalLines,
                    FulfillmentVersion = aggregate.Fulfillment.RowVersion,
                    UpdatedAt = now
                });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return PosOnlineOrderPickingRepositoryResult.Failure("online_orders.concurrency_conflict");
        }
        catch (InvalidOperationException ex) when (ex.Message == "FULFILLMENT_VERSION_CONFLICT")
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return PosOnlineOrderPickingRepositoryResult.Failure("online_orders.concurrency_conflict");
        }
        catch (InvalidOperationException ex) when (ex.Message == "FULFILLMENT_NOT_PICKABLE")
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return PosOnlineOrderPickingRepositoryResult.Failure("online_orders.invalid_state");
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private static async Task<PosOnlineOrderPickingRepositoryResult> RollbackFailureAsync(
        IDbContextTransaction? transaction, string errorCode, CancellationToken cancellationToken)
    {
        if (transaction is not null)
            await transaction.RollbackAsync(cancellationToken);
        return PosOnlineOrderPickingRepositoryResult.Failure(errorCode);
    }
}
