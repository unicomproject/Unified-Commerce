using System.Text.Json;
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

public sealed class PosOnlineOrderPackingRepository : IPosOnlineOrderPackingRepository
{
    public const string PackedEvent = "FULFILLMENT_PACKED";
    public const string ReadyEvent = "FULFILLMENT_READY_FOR_COLLECTION";
    public const string PickupReadyEvent = "PICKUP_READY_FOR_COLLECTION";
    private const string ClickAndCollectOrderType = "CLICK_AND_COLLECT";

    private readonly EPosDbContext _dbContext;

    public PosOnlineOrderPackingRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PosOnlineOrderPackingRepositoryResult> PackAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        Guid orderId,
        PosOnlineOrderPackRequest request,
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

            var aggregate = await LoadAggregateAsync(tenantId, outletId, orderId, cancellationToken);
            if (aggregate is null)
                return await RollbackFailureAsync(transaction, "online_orders.not_found", cancellationToken);

            var fulfillment = aggregate.Fulfillment;
            if (request.ExpectedVersion <= 0 || fulfillment.RowVersion != request.ExpectedVersion)
                return await RollbackFailureAsync(transaction, "online_orders.concurrency_conflict", cancellationToken);
            if (fulfillment.FulfillmentStatus != "PICKING")
                return await RollbackFailureAsync(transaction, "online_orders.invalid_state", cancellationToken);

            var lines = await _dbContext.FulfillmentOrderLines
                .Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == fulfillment.Id)
                .ToListAsync(cancellationToken);
            if (lines.Count == 0 ||
                !lines.All(x => x.PickedQuantity + x.CancelledQuantity >= x.RequestedQuantity))
                return await RollbackFailureAsync(transaction, "online_orders.not_packable", cancellationToken);

            try
            {
                foreach (var line in lines)
                    line.Pack(tenantUserId, now);
                fulfillment.Pack(tenantUserId, request.ExpectedVersion, now);
            }
            catch (InvalidOperationException ex) when (ex.Message == "FULFILLMENT_VERSION_CONFLICT")
            {
                return await RollbackFailureAsync(transaction, "online_orders.concurrency_conflict", cancellationToken);
            }
            catch (InvalidOperationException ex) when (ex.Message is
                "FULFILLMENT_NOT_PACKABLE" or "FULFILLMENT_PACK_NOT_READY" or
                "FULFILLMENT_ALREADY_PACKED" or "FULFILLMENT_PACK_QUANTITY_INVALID" or
                "FULFILLMENT_PACK_QUANTITY_EXCEEDED")
            {
                return await RollbackFailureAsync(transaction, "online_orders.not_packable", cancellationToken);
            }

            MarkFulfillmentMutation(fulfillment, request.ExpectedVersion);
            _dbContext.Entry(fulfillment).Property(x => x.FulfillmentStatus).IsModified = true;
            _dbContext.Entry(fulfillment).Property(x => x.PackedAt).IsModified = true;
            foreach (var line in lines)
            {
                var entry = _dbContext.Entry(line);
                entry.Property(x => x.PackedQuantity).IsModified = true;
                entry.Property(x => x.PackedByTenantUserId).IsModified = true;
                entry.Property(x => x.LineStatus).IsModified = true;
                entry.Property(x => x.UpdatedAt).IsModified = true;
            }

            var sequence = await NextFulfillmentEventSequenceAsync(tenantId, fulfillment.Id, cancellationToken);
            var payload = JsonSerializer.Serialize(new
            {
                packedLineCount = lines.Count,
                packedUnits = lines.Sum(x => x.PackedQuantity),
                hasPackingNote = !string.IsNullOrWhiteSpace(request.PackingNote)
            });
            _dbContext.FulfillmentOrderEvents.Add(FulfillmentOrderEvent.Create(
                Guid.NewGuid(), tenantId, fulfillment.Id, sequence,
                PackedEvent, "PICKING", "PACKED", now, tenantUserId,
                string.IsNullOrWhiteSpace(request.PackingNote) ? "Fulfilment packed" : request.PackingNote,
                payload));

            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return PosOnlineOrderPackingRepositoryResult.Success(new PosOnlineOrderPackReadyCommandResponse
            {
                OrderId = aggregate.Order.Id,
                FulfillmentOrderId = fulfillment.Id,
                Status = fulfillment.FulfillmentStatus,
                TotalLines = lines.Count,
                CompletedLines = lines.Count,
                CanPack = false,
                FulfillmentVersion = fulfillment.RowVersion,
                UpdatedAt = now
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return PosOnlineOrderPackingRepositoryResult.Failure("online_orders.concurrency_conflict");
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

    public async Task<PosOnlineOrderPackingRepositoryResult> MarkReadyAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        Guid orderId,
        PosOnlineOrderReadyRequest request,
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

            var aggregate = await LoadAggregateAsync(tenantId, outletId, orderId, cancellationToken);
            if (aggregate is null)
                return await RollbackFailureAsync(transaction, "online_orders.not_found", cancellationToken);

            var fulfillment = aggregate.Fulfillment;
            var order = aggregate.Order;
            if (request.ExpectedVersion <= 0 || fulfillment.RowVersion != request.ExpectedVersion)
                return await RollbackFailureAsync(transaction, "online_orders.concurrency_conflict", cancellationToken);
            if (fulfillment.FulfillmentStatus != "PACKED")
                return await RollbackFailureAsync(transaction, "online_orders.not_readyable", cancellationToken);

            var lines = await _dbContext.FulfillmentOrderLines
                .Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == fulfillment.Id)
                .ToListAsync(cancellationToken);
            if (lines.Count == 0 ||
                !lines.All(x =>
                {
                    var effectiveRequired = x.RequestedQuantity - x.CancelledQuantity;
                    return x.PickedQuantity + x.CancelledQuantity >= x.RequestedQuantity &&
                           x.PackedQuantity == x.PickedQuantity &&
                           x.PackedQuantity <= effectiveRequired;
                }))
                return await RollbackFailureAsync(transaction, "online_orders.not_readyable", cancellationToken);

            var pickup = await _dbContext.PickupOrders
                .FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId && x.FulfillmentOrderId == fulfillment.Id,
                    cancellationToken);
            if (pickup is null)
                return await RollbackFailureAsync(transaction, "online_orders.invalid_pickup", cancellationToken);

            var oldPickupStatus = pickup.PickupStatus;
            try
            {
                fulfillment.MarkReady(tenantUserId, request.ExpectedVersion, now);
                order.ApplyPosReadyForCollection(tenantUserId, now);
                pickup.MarkReady(now);
            }
            catch (InvalidOperationException ex) when (ex.Message == "FULFILLMENT_VERSION_CONFLICT")
            {
                return await RollbackFailureAsync(transaction, "online_orders.concurrency_conflict", cancellationToken);
            }
            catch (InvalidOperationException ex) when (
                ex.Message is "FULFILLMENT_NOT_READYABLE" or "PICKUP_NOT_READYABLE" or "PICKUP_ALREADY_READY"
                || ex.Message.Contains("ready for collection", StringComparison.OrdinalIgnoreCase))
            {
                return await RollbackFailureAsync(transaction, "online_orders.invalid_state", cancellationToken);
            }

            MarkFulfillmentMutation(fulfillment, request.ExpectedVersion);
            _dbContext.Entry(fulfillment).Property(x => x.FulfillmentStatus).IsModified = true;
            _dbContext.Entry(fulfillment).Property(x => x.ReadyAt).IsModified = true;

            var orderEntry = _dbContext.Entry(order);
            orderEntry.Property(x => x.Status).IsModified = true;
            orderEntry.Property(x => x.FulfillmentStatus).IsModified = true;
            orderEntry.Property(x => x.UpdatedByTenantUserId).IsModified = true;
            orderEntry.Property(x => x.UpdatedAt).IsModified = true;

            var pickupEntry = _dbContext.Entry(pickup);
            pickupEntry.Property(x => x.PickupStatus).IsModified = true;
            pickupEntry.Property(x => x.UpdatedAt).IsModified = true;

            var fulfillmentSequence = await NextFulfillmentEventSequenceAsync(
                tenantId, fulfillment.Id, cancellationToken);
            _dbContext.FulfillmentOrderEvents.Add(FulfillmentOrderEvent.Create(
                Guid.NewGuid(), tenantId, fulfillment.Id, fulfillmentSequence,
                ReadyEvent, "PACKED", "READY", now, tenantUserId,
                "Fulfilment marked ready for collection"));

            var pickupSequence = await _dbContext.PickupOrderEvents
                .Where(x => x.TenantId == tenantId && x.PickupOrderId == pickup.Id)
                .MaxAsync(x => (int?)x.SequenceNumber, cancellationToken) ?? 0;
            _dbContext.PickupOrderEvents.Add(PickupOrderEvent.Create(
                Guid.NewGuid(), tenantId, pickup.Id, pickupSequence + 1,
                PickupReadyEvent, oldPickupStatus, "READY",
                now, tenantUserId, "Pickup marked ready for collection"));

            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return PosOnlineOrderPackingRepositoryResult.Success(new PosOnlineOrderPackReadyCommandResponse
            {
                OrderId = order.Id,
                FulfillmentOrderId = fulfillment.Id,
                Status = fulfillment.FulfillmentStatus,
                TotalLines = lines.Count,
                CompletedLines = lines.Count,
                CanPack = false,
                FulfillmentVersion = fulfillment.RowVersion,
                UpdatedAt = now
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return PosOnlineOrderPackingRepositoryResult.Failure("online_orders.concurrency_conflict");
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

    private async Task<PackingAggregate?> LoadAggregateAsync(
        Guid tenantId, Guid outletId, Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _dbContext.SalesOrders.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.Id == orderId &&
            x.OrderType == ClickAndCollectOrderType && x.ReportingOutletId == outletId,
            cancellationToken);
        if (order is null)
            return null;

        var eligibleFulfillmentIds = _dbContext.FulfillmentMethodOutlets.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.OutletId == outletId)
            .Select(x => x.Id);
        var fulfillment = await _dbContext.FulfillmentOrders
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == orderId &&
                        eligibleFulfillmentIds.Contains(x.FulfillmentMethodOutletId))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return fulfillment is null ? null : new PackingAggregate(order, fulfillment);
    }

    private void MarkFulfillmentMutation(FulfillmentOrder fulfillment, long expectedVersion)
    {
        var entry = _dbContext.Entry(fulfillment);
        if (entry.State == EntityState.Detached)
        {
            var existing = _dbContext.ChangeTracker.Entries<FulfillmentOrder>()
                .FirstOrDefault(e => e.Entity.Id == fulfillment.Id);
            if (existing is not null)
            {
                existing.CurrentValues.SetValues(fulfillment);
                entry = existing;
            }
            else
            {
                _dbContext.Attach(fulfillment);
                entry = _dbContext.Entry(fulfillment);
            }
        }

        entry.Property(x => x.RowVersion).OriginalValue = expectedVersion;
        entry.Property(x => x.RowVersion).IsModified = true;
        entry.Property(x => x.UpdatedByTenantUserId).IsModified = true;
        entry.Property(x => x.UpdatedAt).IsModified = true;
    }

    private async Task<int> NextFulfillmentEventSequenceAsync(
        Guid tenantId, Guid fulfillmentOrderId, CancellationToken cancellationToken) =>
        (await _dbContext.FulfillmentOrderEvents
            .Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == fulfillmentOrderId)
            .MaxAsync(x => (int?)x.SequenceNumber, cancellationToken) ?? 0) + 1;

    private async Task<string?> ValidateAccessAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId, CancellationToken cancellationToken)
    {
        var contextIsActive = await (
            from tenant in _dbContext.Tenants.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking() on tenant.Id equals user.TenantId
            join outlet in _dbContext.Outlets.AsNoTracking() on tenant.Id equals outlet.TenantId
            where tenant.Id == tenantId && tenant.Status == TenantStatusConstants.Active &&
                  user.Id == tenantUserId && user.AccountStatus == TenantUserConstants.StatusActive &&
                  outlet.Id == outletId && outlet.Status == OutletConstants.ActiveStatus
            select outlet.Id).AnyAsync(cancellationToken);
        if (!contextIsActive)
            return "online_orders.outlet_access_denied";

        var scopedOutletIds = _dbContext.OutletUserRoles.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TenantUserId == tenantUserId && x.RevokedAt == null)
            .Select(x => x.OutletId)
            .Union(_dbContext.OutletUserPermissions.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.TenantUserId == tenantUserId && x.RevokedAt == null)
                .Select(x => x.OutletId));
        var hasScope = await scopedOutletIds.AnyAsync(cancellationToken);
        return hasScope && !await scopedOutletIds.ContainsAsync(outletId, cancellationToken)
            ? "online_orders.outlet_access_denied"
            : null;
    }

    private static async Task<PosOnlineOrderPackingRepositoryResult> RollbackFailureAsync(
        IDbContextTransaction? transaction, string errorCode, CancellationToken cancellationToken)
    {
        if (transaction is not null)
            await transaction.RollbackAsync(cancellationToken);
        return PosOnlineOrderPackingRepositoryResult.Failure(errorCode);
    }

    private sealed record PackingAggregate(
        E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder Order,
        FulfillmentOrder Fulfillment);
}
