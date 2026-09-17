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

public sealed class PosOnlineOrderCollectionRepository : IPosOnlineOrderCollectionRepository
{
    public const string FulfilledEvent = "FULFILLMENT_FULFILLED";
    public const string PickupCollectedEvent = "PICKUP_COLLECTED";
    private const string ClickAndCollectOrderType = "CLICK_AND_COLLECT";
    private const decimal PaymentSettledEpsilon = 0.01m;

    private readonly EPosDbContext _dbContext;

    public PosOnlineOrderCollectionRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>> ValidateQrAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateAccessAsync(tenantId, tenantUserId, outletId, cancellationToken);
        if (accessError is not null)
            return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>.Failure(accessError);

        if (string.IsNullOrWhiteSpace(tokenHash))
            return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>
                .Failure("online_orders.collection.qr_invalid");

        var pickup = await _dbContext.PickupOrders.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.PickupQrTokenHash == tokenHash,
                cancellationToken);
        if (pickup is null)
            return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>
                .Failure("online_orders.collection.qr_invalid");

        if (pickup.PickupQrExpiresAt is { } expiresAt && expiresAt < now)
            return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>
                .Failure("online_orders.collection.qr_expired");

        var fulfillment = await _dbContext.FulfillmentOrders.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.Id == pickup.FulfillmentOrderId,
                cancellationToken);
        if (fulfillment is null)
            return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>
                .Failure("online_orders.collection.missing_graph");

        var methodOutlet = await _dbContext.FulfillmentMethodOutlets.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.Id == fulfillment.FulfillmentMethodOutletId,
                cancellationToken);
        if (methodOutlet is null)
            return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>
                .Failure("online_orders.collection.missing_graph");

        if (methodOutlet.OutletId != outletId)
            return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>
                .Failure("online_orders.collection.wrong_outlet");

        var order = await _dbContext.SalesOrders.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Id == fulfillment.SalesOrderId &&
                x.OrderType == ClickAndCollectOrderType,
                cancellationToken);
        if (order is null)
            return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>
                .Failure("online_orders.collection.missing_graph");

        if (order.Status is "CANCELLED" ||
            order.FulfillmentStatus is "CANCELLED" ||
            pickup.PickupStatus is "CANCELLED" ||
            fulfillment.FulfillmentStatus is "CANCELLED")
            return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>
                .Failure("online_orders.collection.cancelled");

        if (pickup.PickupStatus == "COLLECTED" || pickup.CollectedAt.HasValue ||
            order.FulfillmentStatus == "COLLECTED" ||
            fulfillment.FulfillmentStatus == "FULFILLED")
            return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>
                .Failure("online_orders.collection.already_collected");

        if (pickup.PickupStatus != "READY" || fulfillment.FulfillmentStatus != "READY")
            return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>
                .Failure("online_orders.collection.not_ready");

        var outletName = await _dbContext.Outlets.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == outletId)
            .Select(x => x.OutletName)
            .FirstOrDefaultAsync(cancellationToken);

        var items = await (
            from line in _dbContext.FulfillmentOrderLines.AsNoTracking()
            join salesLine in _dbContext.SalesOrderLines.AsNoTracking()
                on new { line.TenantId, Id = line.SalesOrderLineId }
                equals new { salesLine.TenantId, salesLine.Id }
            where line.TenantId == tenantId && line.FulfillmentOrderId == fulfillment.Id
            orderby salesLine.LineNumber
            select new PosOnlineOrderCollectionItemDto
            {
                ProductName = salesLine.ProductNameSnapshot,
                QuantityPacked = line.PackedQuantity
            }).ToListAsync(cancellationToken);

        var canCollect = pickup.PickupStatus == "READY" &&
                         pickup.CollectedAt is null &&
                         fulfillment.FulfillmentStatus == "READY" &&
                         fulfillment.FulfilledAt is null;
        var canTakePayment = order.BalanceDue > PaymentSettledEpsilon;

        return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionValidateResponse>.Success(
            new PosOnlineOrderCollectionValidateResponse
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.CustomerNameSnapshot ?? pickup.PickupContactName,
                CustomerPhone = order.CustomerPhoneSnapshot ?? pickup.PickupContactPhone,
                OutletId = outletId,
                OutletName = outletName,
                CollectionWindowStart = order.RequestedCollectionAt,
                CollectionWindowEnd = order.RequestedCollectionEndAt,
                FulfillmentStatus = fulfillment.FulfillmentStatus,
                PickupStatus = pickup.PickupStatus,
                ReadyAt = fulfillment.ReadyAt,
                CollectedAt = pickup.CollectedAt,
                PaymentStatus = order.PaymentStatus,
                Currency = order.CurrencyCode,
                Total = order.TotalAmount,
                PaidAmount = order.PaidAmount,
                BalanceDue = order.BalanceDue,
                CanCollect = canCollect,
                CanTakePayment = canTakePayment,
                ExpectedVersion = fulfillment.RowVersion,
                PickupNumber = pickup.PickupNumber,
                Items = items
            });
    }

    public async Task<PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionCompleteResponse>> CompleteAsync(
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

            var aggregate = await LoadAggregateAsync(tenantId, outletId, orderId, cancellationToken);
            if (aggregate is null)
                return await RollbackFailureAsync(transaction, "online_orders.not_found", cancellationToken);

            var (order, fulfillment, pickup) = aggregate.Value;

            if (pickup.PickupStatus == "COLLECTED" && pickup.CollectedAt.HasValue &&
                order.Status == "COMPLETED" &&
                order.FulfillmentStatus == "COLLECTED" &&
                fulfillment.FulfillmentStatus == "FULFILLED")
            {
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);

                return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionCompleteResponse>.Success(
                    BuildCompleteResponse(order, fulfillment, pickup, alreadyCollected: true));
            }

            if (order.Status is "CANCELLED" ||
                order.FulfillmentStatus is "CANCELLED" ||
                pickup.PickupStatus is "CANCELLED" ||
                fulfillment.FulfillmentStatus is "CANCELLED")
                return await RollbackFailureAsync(transaction, "online_orders.collection.cancelled", cancellationToken);

            if (pickup.PickupStatus == "COLLECTED" || pickup.CollectedAt.HasValue)
                return await RollbackFailureAsync(transaction, "online_orders.collection.already_collected", cancellationToken);

            if (pickup.PickupStatus != "READY" || fulfillment.FulfillmentStatus != "READY")
                return await RollbackFailureAsync(transaction, "online_orders.collection.not_ready", cancellationToken);

            var paymentSettled =
                order.BalanceDue <= PaymentSettledEpsilon ||
                string.Equals(order.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase);
            if (!paymentSettled)
                return await RollbackFailureAsync(transaction, "online_orders.collection.payment_required", cancellationToken);

            if (expectedVersion <= 0 || fulfillment.RowVersion != expectedVersion)
                return await RollbackFailureAsync(transaction, "online_orders.concurrency_conflict", cancellationToken);

            var oldPickupStatus = pickup.PickupStatus;
            try
            {
                pickup.MarkCollected(now);
                fulfillment.MarkFulfilled(tenantUserId, expectedVersion, now);
                order.ApplyPosCollected(tenantUserId, now);
            }
            catch (InvalidOperationException ex) when (ex.Message == "FULFILLMENT_VERSION_CONFLICT")
            {
                return await RollbackFailureAsync(transaction, "online_orders.concurrency_conflict", cancellationToken);
            }
            catch (InvalidOperationException ex) when (
                ex.Message is "FULFILLMENT_NOT_FULFILLABLE" or "PICKUP_NOT_COLLECTABLE" ||
                ex.Message.Contains("collect", StringComparison.OrdinalIgnoreCase))
            {
                return await RollbackFailureAsync(transaction, "online_orders.invalid_state", cancellationToken);
            }

            MarkFulfillmentMutation(fulfillment, expectedVersion);
            _dbContext.Entry(fulfillment).Property(x => x.FulfillmentStatus).IsModified = true;
            _dbContext.Entry(fulfillment).Property(x => x.FulfilledAt).IsModified = true;

            var orderEntry = _dbContext.Entry(order);
            orderEntry.Property(x => x.Status).IsModified = true;
            orderEntry.Property(x => x.FulfillmentStatus).IsModified = true;
            orderEntry.Property(x => x.CompletedAt).IsModified = true;
            orderEntry.Property(x => x.UpdatedByTenantUserId).IsModified = true;
            orderEntry.Property(x => x.UpdatedAt).IsModified = true;

            var pickupEntry = _dbContext.Entry(pickup);
            pickupEntry.Property(x => x.PickupStatus).IsModified = true;
            pickupEntry.Property(x => x.CollectedAt).IsModified = true;
            pickupEntry.Property(x => x.UpdatedAt).IsModified = true;

            var fulfillmentSequence = await NextFulfillmentEventSequenceAsync(
                tenantId, fulfillment.Id, cancellationToken);
            _dbContext.FulfillmentOrderEvents.Add(FulfillmentOrderEvent.Create(
                Guid.NewGuid(), tenantId, fulfillment.Id, fulfillmentSequence,
                FulfilledEvent, "READY", "FULFILLED", now, tenantUserId,
                "Fulfilment marked fulfilled after customer collection"));

            var pickupSequence = await _dbContext.PickupOrderEvents
                .Where(x => x.TenantId == tenantId && x.PickupOrderId == pickup.Id)
                .MaxAsync(x => (int?)x.SequenceNumber, cancellationToken) ?? 0;
            _dbContext.PickupOrderEvents.Add(PickupOrderEvent.Create(
                Guid.NewGuid(), tenantId, pickup.Id, pickupSequence + 1,
                PickupCollectedEvent, oldPickupStatus, "COLLECTED",
                now, tenantUserId, "Pickup marked collected"));

            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionCompleteResponse>.Success(
                BuildCompleteResponse(order, fulfillment, pickup, alreadyCollected: false));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionCompleteResponse>
                .Failure("online_orders.concurrency_conflict");
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

    private async Task<(Domain.Modules.Tenant.Orders.Entities.SalesOrder Order, FulfillmentOrder Fulfillment, PickupOrder Pickup)?> LoadAggregateAsync(
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
        if (fulfillment is null)
            return null;

        var pickup = await _dbContext.PickupOrders.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId && x.FulfillmentOrderId == fulfillment.Id,
            cancellationToken);
        return pickup is null ? null : (order, fulfillment, pickup);
    }

    private static PosOnlineOrderCollectionCompleteResponse BuildCompleteResponse(
        Domain.Modules.Tenant.Orders.Entities.SalesOrder order,
        FulfillmentOrder fulfillment,
        PickupOrder pickup,
        bool alreadyCollected) =>
        new()
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            PickupStatus = pickup.PickupStatus,
            FulfillmentStatus = fulfillment.FulfillmentStatus,
            SalesOrderStatus = order.Status,
            SalesFulfillmentStatus = order.FulfillmentStatus,
            CollectedAt = pickup.CollectedAt,
            FulfilledAt = fulfillment.FulfilledAt,
            CompletedAt = order.CompletedAt,
            FulfillmentVersion = fulfillment.RowVersion,
            AlreadyCollected = alreadyCollected
        };

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

    private static async Task<PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionCompleteResponse>> RollbackFailureAsync(
        IDbContextTransaction? transaction, string errorCode, CancellationToken cancellationToken)
    {
        if (transaction is not null)
            await transaction.RollbackAsync(cancellationToken);
        return PosOnlineOrderCollectionRepositoryResult<PosOnlineOrderCollectionCompleteResponse>.Failure(errorCode);
    }
}
