using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;

public sealed partial class PosOnlineOrderPickingRepository
{
    private async Task<PickingAggregate?> LoadAggregateAsync(
        Guid tenantId, Guid outletId, Guid orderId, bool tracked,
        CancellationToken cancellationToken)
    {
        var orderQuery = DbContext.SalesOrders.Where(x =>
            x.TenantId == tenantId && x.Id == orderId &&
            x.OrderType == ClickAndCollectOrderType && x.ReportingOutletId == outletId);
        var order = tracked
            ? await orderQuery.FirstOrDefaultAsync(cancellationToken)
            : await orderQuery.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (order is null)
            return null;

        var eligibleFulfillmentIds = DbContext.FulfillmentMethodOutlets.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.OutletId == outletId)
            .Select(x => x.Id);
        var fulfillmentQuery = DbContext.FulfillmentOrders
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == orderId &&
                        eligibleFulfillmentIds.Contains(x.FulfillmentMethodOutletId))
            .OrderByDescending(x => x.CreatedAt);
        var fulfillment = tracked
            ? await fulfillmentQuery.FirstOrDefaultAsync(cancellationToken)
            : await fulfillmentQuery.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return fulfillment is null ? null : new PickingAggregate(order, fulfillment);
    }

    private void MarkPickingChanges(
        FulfillmentOrder fulfillment, FulfillmentOrderLine line, long expectedVersion)
    {
        MarkFulfillmentMutation(fulfillment, expectedVersion);
        var entry = DbContext.Entry(line);
        if (entry.State == EntityState.Detached)
            DbContext.Attach(line);
        entry.Property(x => x.PickedQuantity).IsModified = true;
        entry.Property(x => x.PickedByTenantUserId).IsModified = true;
        entry.Property(x => x.LineStatus).IsModified = true;
        entry.Property(x => x.UpdatedAt).IsModified = true;
    }

    private void MarkFulfillmentMutation(FulfillmentOrder fulfillment, long expectedVersion)
    {
        var entry = DbContext.Entry(fulfillment);
        if (entry.State == EntityState.Detached)
            DbContext.Attach(fulfillment);
        entry.Property(x => x.RowVersion).OriginalValue = expectedVersion;
        entry.Property(x => x.RowVersion).IsModified = true;
        entry.Property(x => x.UpdatedByTenantUserId).IsModified = true;
        entry.Property(x => x.UpdatedAt).IsModified = true;
    }

    private async Task<string?> ValidateAccessAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId,
        CancellationToken cancellationToken)
    {
        var contextIsActive = await (
            from tenant in DbContext.Tenants.AsNoTracking()
            join user in DbContext.TenantUsers.AsNoTracking() on tenant.Id equals user.TenantId
            join outlet in DbContext.Outlets.AsNoTracking() on tenant.Id equals outlet.TenantId
            where tenant.Id == tenantId && tenant.Status == TenantStatusConstants.Active &&
                  user.Id == tenantUserId && user.AccountStatus == TenantUserConstants.StatusActive &&
                  outlet.Id == outletId && outlet.Status == OutletConstants.ActiveStatus
            select outlet.Id).AnyAsync(cancellationToken);
        if (!contextIsActive)
            return "online_orders.outlet_access_denied";

        var scopedOutletIds = DbContext.OutletUserRoles.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TenantUserId == tenantUserId && x.RevokedAt == null)
            .Select(x => x.OutletId)
            .Union(DbContext.OutletUserPermissions.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.TenantUserId == tenantUserId && x.RevokedAt == null)
                .Select(x => x.OutletId));
        var hasScope = await scopedOutletIds.AnyAsync(cancellationToken);
        return hasScope && !await scopedOutletIds.ContainsAsync(outletId, cancellationToken)
            ? "online_orders.outlet_access_denied"
            : null;
    }

    private sealed record PickingAggregate(
        E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder Order,
        FulfillmentOrder Fulfillment);

    private sealed record MutationResult(bool IsSuccess, bool CanPack, string? ErrorCode)
    {
        public static MutationResult Success(bool canPack) => new(true, canPack, null);
        public static MutationResult Failure(string errorCode) => new(false, false, errorCode);
    }
}
