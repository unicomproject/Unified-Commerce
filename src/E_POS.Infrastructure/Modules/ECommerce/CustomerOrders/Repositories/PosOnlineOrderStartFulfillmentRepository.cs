using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;

public sealed class PosOnlineOrderStartFulfillmentRepository : IPosOnlineOrderStartFulfillmentRepository
{
    private const string ClickAndCollectOrderType = "CLICK_AND_COLLECT";
    private readonly EPosDbContext _dbContext;

    public PosOnlineOrderStartFulfillmentRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PosOnlineOrderStartFulfillmentRepositoryResult> StartAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        Guid orderId,
        long expectedVersion,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = null;
        if (_dbContext.Database.IsRelational())
            transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var accessError = await ValidateAccessAsync(tenantId, tenantUserId, outletId, cancellationToken);
            if (accessError is not null)
                return await RollbackFailureAsync(transaction, accessError, cancellationToken);

            var aggregate = await (
                from order in _dbContext.SalesOrders
                join fulfillmentRow in _dbContext.FulfillmentOrders
                    on new { order.TenantId, SalesOrderId = order.Id }
                    equals new { fulfillmentRow.TenantId, fulfillmentRow.SalesOrderId }
                join methodOutlet in _dbContext.FulfillmentMethodOutlets
                    on new { fulfillmentRow.TenantId, Id = fulfillmentRow.FulfillmentMethodOutletId }
                    equals new { methodOutlet.TenantId, methodOutlet.Id }
                where order.TenantId == tenantId &&
                      order.Id == orderId &&
                      order.OrderType == ClickAndCollectOrderType &&
                      order.ReportingOutletId == outletId &&
                      methodOutlet.OutletId == outletId
                select new { Order = order, Fulfillment = fulfillmentRow })
                .OrderByDescending(x => x.Fulfillment.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (aggregate is null)
                return await RollbackFailureAsync(transaction, "online_orders.not_found", cancellationToken);

            var fulfillment = aggregate.Fulfillment;
            if (fulfillment.RowVersion != expectedVersion)
                return await RollbackFailureAsync(transaction, "online_orders.concurrency_conflict", cancellationToken);

            if (aggregate.Order.Status is not ("CONFIRMED" or "ACCEPTED"))
                return await RollbackFailureAsync(transaction, "online_orders.invalid_state", cancellationToken);

            if (fulfillment.FulfillmentStatus is not ("PENDING" or "ALLOCATED"))
                return await RollbackFailureAsync(transaction, "online_orders.invalid_state", cancellationToken);

            var pickup = await _dbContext.PickupOrders
                .FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId &&
                    x.FulfillmentOrderId == fulfillment.Id,
                    cancellationToken);
            if (pickup is null || pickup.PickupStatus != "PENDING" || !pickup.PickupSlotReservationId.HasValue)
                return await RollbackFailureAsync(transaction, "online_orders.invalid_reservation", cancellationToken);

            var pickupReservationValid = await (
                from reservation in _dbContext.PickupSlotReservations
                join slot in _dbContext.PickupSlots
                    on new { reservation.TenantId, Id = reservation.PickupSlotId }
                    equals new { slot.TenantId, slot.Id }
                join reservationMethodOutlet in _dbContext.FulfillmentMethodOutlets
                    on new { slot.TenantId, Id = slot.FulfillmentMethodOutletId }
                    equals new { reservationMethodOutlet.TenantId, reservationMethodOutlet.Id }
                where reservation.TenantId == tenantId &&
                      reservation.Id == pickup.PickupSlotReservationId.Value &&
                      reservation.SalesOrderId == orderId &&
                      reservation.ReservationStatus == "CONFIRMED" &&
                      reservationMethodOutlet.OutletId == outletId
                select reservation.Id)
                .AnyAsync(cancellationToken);
            var inventoryReservationValid = await _dbContext.InventoryReservations.AnyAsync(x =>
                x.TenantId == tenantId &&
                x.SourceReferenceId == orderId &&
                x.FulfillmentOutletId == outletId &&
                x.ReservationStatus == "CONFIRMED" &&
                (!x.ExpiresAt.HasValue || x.ExpiresAt > now),
                cancellationToken);

            if (!pickupReservationValid || !inventoryReservationValid)
                return await RollbackFailureAsync(transaction, "online_orders.invalid_reservation", cancellationToken);

            var oldStatus = fulfillment.FulfillmentStatus;
            try
            {
                fulfillment.StartPicking(tenantUserId, expectedVersion, now);
            }
            catch (InvalidOperationException ex) when (ex.Message == "FULFILLMENT_VERSION_CONFLICT")
            {
                return await RollbackFailureAsync(transaction, "online_orders.concurrency_conflict", cancellationToken);
            }
            catch (InvalidOperationException ex) when (ex.Message == "FULFILLMENT_NOT_STARTABLE")
            {
                return await RollbackFailureAsync(transaction, "online_orders.invalid_state", cancellationToken);
            }

            var sequence = await _dbContext.FulfillmentOrderEvents
                .Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == fulfillment.Id)
                .MaxAsync(x => (int?)x.SequenceNumber, cancellationToken) ?? 0;
            _dbContext.FulfillmentOrderEvents.Add(FulfillmentOrderEvent.Create(
                Guid.NewGuid(), tenantId, fulfillment.Id, sequence + 1, "FULFILLMENT_STARTED",
                oldStatus, "PICKING", now, tenantUserId, "POS Start Fulfilment"));

            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return PosOnlineOrderStartFulfillmentRepositoryResult.Success(
                new PosOnlineOrderStartFulfillmentResponse
                {
                    OrderId = aggregate.Order.Id,
                    FulfillmentOrderId = fulfillment.Id,
                    FulfillmentNumber = fulfillment.FulfillmentNumber,
                    FulfillmentStatus = fulfillment.FulfillmentStatus,
                    AssignedToTenantUserId = tenantUserId,
                    StartedAt = now,
                    FulfillmentVersion = fulfillment.RowVersion
                });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return PosOnlineOrderStartFulfillmentRepositoryResult.Failure("online_orders.concurrency_conflict");
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

    private async Task<string?> ValidateAccessAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var contextIsActive = await (
            from tenant in _dbContext.Tenants.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking() on tenant.Id equals user.TenantId
            join outlet in _dbContext.Outlets.AsNoTracking() on tenant.Id equals outlet.TenantId
            where tenant.Id == tenantId &&
                  tenant.Status == TenantStatusConstants.Active &&
                  user.Id == tenantUserId &&
                  user.AccountStatus == TenantUserConstants.StatusActive &&
                  outlet.Id == outletId &&
                  outlet.Status == OutletConstants.ActiveStatus
            select outlet.Id).AnyAsync(cancellationToken);
        if (!contextIsActive)
            return "online_orders.outlet_access_denied";

        var scopedOutletIds = _dbContext.OutletUserRoles.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TenantUserId == tenantUserId && x.RevokedAt == null)
            .Select(x => x.OutletId)
            .Union(_dbContext.OutletUserPermissions.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.TenantUserId == tenantUserId && x.RevokedAt == null)
                .Select(x => x.OutletId));
        var hasScopedAssignment = await scopedOutletIds.AnyAsync(cancellationToken);
        return hasScopedAssignment && !await scopedOutletIds.ContainsAsync(outletId, cancellationToken)
            ? "online_orders.outlet_access_denied"
            : null;
    }

    private static async Task<PosOnlineOrderStartFulfillmentRepositoryResult> RollbackFailureAsync(
        IDbContextTransaction? transaction,
        string errorCode,
        CancellationToken cancellationToken)
    {
        if (transaction is not null)
            await transaction.RollbackAsync(cancellationToken);
        return PosOnlineOrderStartFulfillmentRepositoryResult.Failure(errorCode);
    }
}
