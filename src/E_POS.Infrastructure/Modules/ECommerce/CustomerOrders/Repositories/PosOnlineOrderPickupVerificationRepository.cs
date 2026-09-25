using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Modules.ECommerce.FulfilmentPickup;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;

public sealed class PosOnlineOrderPickupVerificationRepository : IPosOnlineOrderPickupVerificationRepository
{
    public const string VerifiedEvent = "PICKUP_QR_VERIFIED";
    public const string VerificationFailedEvent = "PICKUP_QR_VERIFICATION_FAILED";
    public const string VerificationLockedEvent = "PICKUP_QR_VERIFICATION_LOCKED";
    public const string CollectedEvent = "PICKUP_COLLECTED";
    public const string QrVerificationMethod = "QR";

    private const string ClickAndCollectOrderType = "CLICK_AND_COLLECT";

    private readonly EPosDbContext _dbContext;

    public PosOnlineOrderPickupVerificationRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PosOnlineOrderPickupVerifyRepositoryResult> VerifyAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        Guid orderId,
        string pickupCode,
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
                return await RollbackVerifyFailureAsync(transaction, accessError, cancellationToken);

            var aggregate = await LoadAggregateAsync(tenantId, outletId, orderId, cancellationToken);
            if (aggregate is null)
                return await RollbackVerifyFailureAsync(transaction, "online_orders.not_found", cancellationToken);

            var pickup = aggregate.Pickup;
            if (pickup.PickupStatus != "READY")
                return await RollbackVerifyFailureAsync(transaction, "online_orders.invalid_state", cancellationToken);
            if (pickup.IsLockedOut)
                return await RollbackVerifyFailureAsync(
                    transaction, "online_orders.pickup_verification_locked", cancellationToken);

            var matched = PickupCodeGenerator.Matches(pickup.PickupQrTokenHash, pickupCode);
            var sequence = await NextPickupEventSequenceAsync(tenantId, pickup.Id, cancellationToken);

            if (!matched)
            {
                var oldStatus = pickup.PickupStatus;
                pickup.RecordFailedVerification(now);
                MarkPickupModified(pickup, verificationFields: true);

                var lockedOut = pickup.IsLockedOut;
                _dbContext.PickupOrderEvents.Add(PickupOrderEvent.Create(
                    Guid.NewGuid(), tenantId, pickup.Id, sequence,
                    lockedOut ? VerificationLockedEvent : VerificationFailedEvent,
                    oldStatus, pickup.PickupStatus, now, tenantUserId,
                    lockedOut
                        ? "Pickup verification locked after repeated failed attempts"
                        : "Pickup code did not match"));

                await _dbContext.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);

                return lockedOut
                    ? PosOnlineOrderPickupVerifyRepositoryResult.Failure("online_orders.pickup_verification_locked")
                    : PosOnlineOrderPickupVerifyRepositoryResult.Failure(
                        "online_orders.pickup_code_mismatch",
                        PickupOrder.MaxVerificationAttempts - pickup.FailedVerificationAttempts);
            }

            var oldPickupStatus = pickup.PickupStatus;
            pickup.Verify(tenantUserId, QrVerificationMethod, now);
            MarkPickupModified(pickup, verificationFields: true);

            _dbContext.PickupOrderEvents.Add(PickupOrderEvent.Create(
                Guid.NewGuid(), tenantId, pickup.Id, sequence,
                VerifiedEvent, oldPickupStatus, pickup.PickupStatus, now, tenantUserId,
                "Pickup code verified at outlet"));

            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return PosOnlineOrderPickupVerifyRepositoryResult.Success(new PosOnlineOrderPickupVerifyResponse
            {
                OrderId = aggregate.Order.Id,
                PickupOrderId = pickup.Id,
                PickupStatus = pickup.PickupStatus,
                VerifiedAt = now,
                RemainingAttempts = PickupOrder.MaxVerificationAttempts
            });
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

    public async Task<PosOnlineOrderPickupCollectRepositoryResult> CollectAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        Guid orderId,
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
                return await RollbackCollectFailureAsync(transaction, accessError, cancellationToken);

            var aggregate = await LoadAggregateAsync(tenantId, outletId, orderId, cancellationToken);
            if (aggregate is null)
                return await RollbackCollectFailureAsync(transaction, "online_orders.not_found", cancellationToken);

            var pickup = aggregate.Pickup;
            var order = aggregate.Order;
            if (pickup.PickupStatus != "VERIFIED")
                return await RollbackCollectFailureAsync(transaction, "online_orders.invalid_state", cancellationToken);

            var oldPickupStatus = pickup.PickupStatus;
            var oldOrderStatus = order.Status;
            var oldFulfillmentStatus = order.FulfillmentStatus;
            try
            {
                pickup.MarkCollected(now);
                order.UpdateClickAndCollectStatus("COMPLETED", tenantUserId, now);
            }
            catch (InvalidOperationException)
            {
                return await RollbackCollectFailureAsync(transaction, "online_orders.invalid_state", cancellationToken);
            }

            MarkPickupModified(pickup, verificationFields: false, collected: true);
            var orderEntry = _dbContext.Entry(order);
            orderEntry.Property(x => x.Status).IsModified = true;
            orderEntry.Property(x => x.FulfillmentStatus).IsModified = true;
            orderEntry.Property(x => x.UpdatedByTenantUserId).IsModified = true;
            orderEntry.Property(x => x.UpdatedAt).IsModified = true;
            orderEntry.Property(x => x.CompletedAt).IsModified = true;

            var sequence = await NextPickupEventSequenceAsync(tenantId, pickup.Id, cancellationToken);
            _dbContext.PickupOrderEvents.Add(PickupOrderEvent.Create(
                Guid.NewGuid(), tenantId, pickup.Id, sequence,
                CollectedEvent, oldPickupStatus, pickup.PickupStatus, now, tenantUserId,
                "Order handed over to customer at outlet"));

            var orderSequence = await _dbContext.SalesOrderStatusHistory
                .Where(x => x.TenantId == tenantId && x.SalesOrderId == order.Id)
                .MaxAsync(x => (int?)x.SequenceNumber, cancellationToken) ?? 0;
            if (!string.Equals(oldOrderStatus, order.Status, StringComparison.OrdinalIgnoreCase))
                _dbContext.SalesOrderStatusHistory.Add(SalesOrderStatusHistory(
                    tenantId, order.Id, ++orderSequence, "ORDER_STATUS",
                    oldOrderStatus, order.Status, tenantUserId, now));
            if (!string.Equals(oldFulfillmentStatus, order.FulfillmentStatus, StringComparison.OrdinalIgnoreCase))
                _dbContext.SalesOrderStatusHistory.Add(SalesOrderStatusHistory(
                    tenantId, order.Id, ++orderSequence, "FULFILLMENT_STATUS",
                    oldFulfillmentStatus, order.FulfillmentStatus, tenantUserId, now));

            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return PosOnlineOrderPickupCollectRepositoryResult.Success(new PosOnlineOrderPickupCollectResponse
            {
                OrderId = order.Id,
                PickupOrderId = pickup.Id,
                PickupStatus = pickup.PickupStatus,
                OrderStatus = order.Status,
                CollectedAt = now
            });
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

    private static E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrderStatusHistory SalesOrderStatusHistory(
        Guid tenantId, Guid orderId, int sequence, string statusType,
        string oldStatus, string newStatus, Guid tenantUserId, DateTimeOffset now) =>
        E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrderStatusHistory.Create(
            Guid.NewGuid(), tenantId, orderId, sequence, statusType,
            oldStatus, newStatus, tenantUserId, now, "Order collected at outlet");

    private async Task<PickupVerificationAggregate?> LoadAggregateAsync(
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
        return pickup is null ? null : new PickupVerificationAggregate(order, pickup);
    }

    private void MarkPickupModified(PickupOrder pickup, bool verificationFields, bool collected = false)
    {
        var entry = _dbContext.Entry(pickup);
        entry.Property(x => x.PickupStatus).IsModified = true;
        entry.Property(x => x.UpdatedAt).IsModified = true;
        if (verificationFields)
        {
            entry.Property(x => x.PickupQrTokenHash).IsModified = true;
            entry.Property(x => x.FailedVerificationAttempts).IsModified = true;
            entry.Property(x => x.VerificationMethod).IsModified = true;
            entry.Property(x => x.VerifiedByTenantUserId).IsModified = true;
            entry.Property(x => x.VerifiedAt).IsModified = true;
        }
        if (collected)
            entry.Property(x => x.CollectedAt).IsModified = true;
    }

    private async Task<int> NextPickupEventSequenceAsync(
        Guid tenantId, Guid pickupOrderId, CancellationToken cancellationToken) =>
        (await _dbContext.PickupOrderEvents
            .Where(x => x.TenantId == tenantId && x.PickupOrderId == pickupOrderId)
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

    private static async Task<PosOnlineOrderPickupVerifyRepositoryResult> RollbackVerifyFailureAsync(
        IDbContextTransaction? transaction, string errorCode, CancellationToken cancellationToken)
    {
        if (transaction is not null)
            await transaction.RollbackAsync(cancellationToken);
        return PosOnlineOrderPickupVerifyRepositoryResult.Failure(errorCode);
    }

    private static async Task<PosOnlineOrderPickupCollectRepositoryResult> RollbackCollectFailureAsync(
        IDbContextTransaction? transaction, string errorCode, CancellationToken cancellationToken)
    {
        if (transaction is not null)
            await transaction.RollbackAsync(cancellationToken);
        return PosOnlineOrderPickupCollectRepositoryResult.Failure(errorCode);
    }

    private sealed record PickupVerificationAggregate(
        E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder Order,
        PickupOrder Pickup);
}
