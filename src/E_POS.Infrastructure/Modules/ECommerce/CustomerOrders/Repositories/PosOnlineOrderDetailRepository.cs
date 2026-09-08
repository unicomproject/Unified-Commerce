using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;

public sealed class PosOnlineOrderDetailRepository : CustomerOrderRepositoryBase, IPosOnlineOrderDetailRepository
{
    public PosOnlineOrderDetailRepository(
        EPosDbContext dbContext,
        IMediaReadUrlResolver? mediaReadUrlResolver = null)
        : base(dbContext, mediaReadUrlResolver)
    {
    }

    public async Task<PosOnlineOrderListRepositoryResult> ListAsync(
        Guid tenantId,
        Guid tenantUserId,
        PosOnlineOrderListQuery request,
        DateTimeOffset serverTime,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateOutletAccessAsync(
            tenantId, tenantUserId, request.OutletId, cancellationToken);
        if (accessError is not null)
            return PosOnlineOrderListRepositoryResult.Failure(accessError);

        var query = DbContext.SalesOrders.AsNoTracking().Where(order =>
            order.TenantId == tenantId &&
            order.OrderType == ClickAndCollectOrderType &&
            order.ReportingOutletId == request.OutletId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = $"%{request.Search.Trim()}%";
            query = query.Where(order =>
                EF.Functions.ILike(order.OrderNumber, search) ||
                (order.ExternalOrderReference != null && EF.Functions.ILike(order.ExternalOrderReference, search)) ||
                (order.CustomerNameSnapshot != null && EF.Functions.ILike(order.CustomerNameSnapshot, search)) ||
                (order.CustomerPhoneSnapshot != null && EF.Functions.ILike(order.CustomerPhoneSnapshot, search)));
        }

        var cancelled = query.Where(order =>
            order.Status == "CANCELLED" || order.FulfillmentStatus == "CANCELLED");
        var collected = query.Where(order =>
            order.Status == "COMPLETED" || order.FulfillmentStatus == "FULFILLED" ||
            order.FulfillmentStatus == "COLLECTED");
        var ready = query.Where(order =>
            order.Status != "CANCELLED" && order.FulfillmentStatus != "CANCELLED" &&
            order.Status != "COMPLETED" && order.FulfillmentStatus != "FULFILLED" &&
            order.FulfillmentStatus != "COLLECTED" &&
            (order.FulfillmentStatus == "READY" || order.FulfillmentStatus == "READY_FOR_COLLECTION" ||
             order.FulfillmentStatus == "READY_FOR_PICKUP"));
        var delayed = query.Where(order =>
            order.Status != "CANCELLED" && order.FulfillmentStatus != "CANCELLED" &&
            order.Status != "COMPLETED" && order.FulfillmentStatus != "FULFILLED" &&
            order.FulfillmentStatus != "COLLECTED" && order.FulfillmentStatus != "READY" &&
            order.FulfillmentStatus != "READY_FOR_COLLECTION" && order.FulfillmentStatus != "READY_FOR_PICKUP" &&
            (order.RequestedCollectionEndAt ?? order.RequestedCollectionAt) < serverTime);
        var preparing = query.Where(order =>
            order.Status != "CANCELLED" && order.FulfillmentStatus != "CANCELLED" &&
            order.Status != "COMPLETED" && order.FulfillmentStatus != "FULFILLED" &&
            order.FulfillmentStatus != "COLLECTED" && order.FulfillmentStatus != "READY" &&
            order.FulfillmentStatus != "READY_FOR_COLLECTION" && order.FulfillmentStatus != "READY_FOR_PICKUP" &&
            (order.RequestedCollectionEndAt ?? order.RequestedCollectionAt) >= serverTime &&
            (order.FulfillmentStatus == "PREPARING" || order.FulfillmentStatus == "PARTIALLY_FULFILLED"));
        var newOrders = query.Where(order =>
            order.Status != "CANCELLED" && order.FulfillmentStatus != "CANCELLED" &&
            order.Status != "COMPLETED" && order.FulfillmentStatus != "FULFILLED" &&
            order.FulfillmentStatus != "COLLECTED" && order.FulfillmentStatus != "READY" &&
            order.FulfillmentStatus != "READY_FOR_COLLECTION" && order.FulfillmentStatus != "READY_FOR_PICKUP" &&
            order.FulfillmentStatus != "PREPARING" && order.FulfillmentStatus != "PARTIALLY_FULFILLED" &&
            !((order.RequestedCollectionEndAt ?? order.RequestedCollectionAt) < serverTime));

        var summary = new PosOnlineOrderSummaryResponse(
            await newOrders.CountAsync(cancellationToken),
            await preparing.CountAsync(cancellationToken),
            await ready.CountAsync(cancellationToken),
            await delayed.CountAsync(cancellationToken),
            await collected.CountAsync(cancellationToken),
            await cancelled.CountAsync(cancellationToken));

        query = Normalize(request.Status) switch
        {
            "NEW" or "PENDING_CONFIRMATION" or "ACCEPTED" => newOrders,
            "PREPARING" => preparing,
            "READY" or "READY_FOR_COLLECTION" => ready,
            "DELAYED" or "OVERDUE" => delayed,
            "COLLECTED" or "COMPLETED" => collected,
            "CANCELLED" => cancelled,
            _ => query
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var descending = string.Equals(request.SortDirection?.Trim(), "desc", StringComparison.OrdinalIgnoreCase);
        query = (request.SortBy?.Trim().ToLowerInvariant(), descending) switch
        {
            ("collectiontime" or "collection_time", true) => query.OrderByDescending(x => x.RequestedCollectionAt),
            ("placedat" or "placed_at", true) => query.OrderByDescending(x => x.PlacedAt),
            ("placedat" or "placed_at", false) => query.OrderBy(x => x.PlacedAt),
            ("amount", true) => query.OrderByDescending(x => x.TotalAmount),
            ("amount", false) => query.OrderBy(x => x.TotalAmount),
            ("customer", true) => query.OrderByDescending(x => x.CustomerNameSnapshot),
            ("customer", false) => query.OrderBy(x => x.CustomerNameSnapshot),
            _ => query.OrderBy(x => x.RequestedCollectionAt).ThenBy(x => x.OrderNumber)
        };

        var orders = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var orderIds = orders.Select(x => x.Id).ToArray();
        var lines = await DbContext.SalesOrderLines.AsNoTracking()
            .Where(line => line.TenantId == tenantId && line.SalesOrderId.HasValue &&
                           orderIds.Contains(line.SalesOrderId.Value) && line.LineStatus != "CANCELLED" &&
                           line.Quantity > line.CancelledQuantity)
            .OrderBy(line => line.LineNumber)
            .ToListAsync(cancellationToken);
        var images = await BuildImageLookupAsync(
            tenantId, lines.Select(x => x.ProductId).Distinct().ToList(), cancellationToken);

        var items = orders.Select(order =>
        {
            var orderLines = lines.Where(line => line.SalesOrderId == order.Id).ToList();
            var previews = orderLines.Take(4).Select(line => new PosOnlineOrderProductPreviewResponse(
                line.ProductId,
                line.ProductVariantId,
                line.ProductNameSnapshot,
                images.GetValueOrDefault(line.ProductId),
                line.ProductNameSnapshot)).ToList();
            var status = DisplayStatus(order, serverTime);
            return new PosOnlineOrderListItemResponse(
                order.Id,
                order.OrderNumber,
                order.ExternalOrderReference,
                order.CustomerNameSnapshot ?? "Walk-in Customer",
                order.CustomerPhoneSnapshot,
                order.RequestedCollectionAt,
                order.RequestedCollectionEndAt,
                order.CollectionTimezoneSnapshot,
                status,
                DisplayStatusLabel(status),
                order.PaymentStatus,
                order.CurrencyCode,
                order.TotalAmount,
                orderLines.Count,
                orderLines.Sum(line => line.Quantity - line.CancelledQuantity),
                previews,
                Math.Max(0, orderLines.Count - previews.Count),
                order.PlacedAt,
                order.UpdatedAt ?? order.CreatedAt);
        }).ToList();

        return PosOnlineOrderListRepositoryResult.Success(new PosOnlineOrderListResponse(
            items, summary, request.Page, request.PageSize, totalCount,
            CalculateTotalPages(totalCount, request.PageSize), serverTime));
    }

    public async Task<PosOnlineOrderDetailRepositoryResult> GetAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        Guid orderId,
        DateTimeOffset serverTime,
        CancellationToken cancellationToken)
    {
        var contextIsActive = await (
            from tenant in DbContext.Tenants.AsNoTracking()
            join user in DbContext.TenantUsers.AsNoTracking()
                on tenant.Id equals user.TenantId
            join outlet in DbContext.Outlets.AsNoTracking()
                on tenant.Id equals outlet.TenantId
            where tenant.Id == tenantId &&
                  tenant.Status == TenantStatusConstants.Active &&
                  user.Id == tenantUserId &&
                  user.AccountStatus == TenantUserConstants.StatusActive &&
                  outlet.Id == outletId &&
                  outlet.Status == OutletConstants.ActiveStatus
            select outlet.Id)
            .AnyAsync(cancellationToken);

        if (!contextIsActive)
            return PosOnlineOrderDetailRepositoryResult.Failure("online_orders.outlet_access_denied");

        var scopedOutletIds = DbContext.OutletUserRoles.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TenantUserId == tenantUserId && x.RevokedAt == null)
            .Select(x => x.OutletId)
            .Union(DbContext.OutletUserPermissions.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.TenantUserId == tenantUserId && x.RevokedAt == null)
                .Select(x => x.OutletId));

        var hasScopedAssignment = await scopedOutletIds.AnyAsync(cancellationToken);
        if (hasScopedAssignment && !await scopedOutletIds.ContainsAsync(outletId, cancellationToken))
            return PosOnlineOrderDetailRepositoryResult.Failure("online_orders.outlet_access_denied");

        var order = await DbContext.SalesOrders.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Id == orderId &&
                x.OrderType == ClickAndCollectOrderType &&
                x.ReportingOutletId == outletId,
                cancellationToken);

        if (order is null)
            return PosOnlineOrderDetailRepositoryResult.Failure("online_orders.not_found");

        var lines = await DbContext.SalesOrderLines.AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.SalesOrderId == orderId &&
                x.LineStatus != "CANCELLED")
            .OrderBy(x => x.LineNumber)
            .ToListAsync(cancellationToken);

        var fulfillment = await DbContext.FulfillmentOrders.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == orderId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var fulfillmentLines = fulfillment is null
            ? []
            : await DbContext.FulfillmentOrderLines.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == fulfillment.Id)
                .ToListAsync(cancellationToken);

        var pickup = fulfillment is null
            ? null
            : await DbContext.PickupOrders.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId &&
                    x.FulfillmentOrderId == fulfillment.Id,
                    cancellationToken);

        var salesChannel = await DbContext.SalesChannels.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == order.SalesChannelId)
            .Select(x => x.CustomName)
            .FirstOrDefaultAsync(cancellationToken);

        var imageLookup = await BuildImageLookupAsync(
            tenantId,
            lines.Select(x => x.ProductId).Distinct().ToList(),
            cancellationToken);
        var fulfillmentLineLookup = fulfillmentLines.ToDictionary(x => x.SalesOrderLineId);
        var displayStatus = MapUiStatus(order);

        var responseLines = lines.Select(line =>
        {
            fulfillmentLineLookup.TryGetValue(line.Id, out var fulfillmentLine);
            var requested = fulfillmentLine?.RequestedQuantity ?? line.Quantity;
            var picked = fulfillmentLine?.PickedQuantity ?? 0m;
            var cancelled = fulfillmentLine?.CancelledQuantity ?? line.CancelledQuantity;
            var remaining = Math.Max(requested - picked - cancelled, 0m);

            return new PosOnlineOrderDetailLineResponse
            {
                Id = line.Id,
                SalesOrderLineId = line.Id,
                FulfillmentOrderLineId = fulfillmentLine?.Id,
                LineNumber = line.LineNumber,
                ProductId = line.ProductId,
                ProductVariantId = line.ProductVariantId,
                ProductName = line.ProductNameSnapshot,
                VariantName = line.VariantNameSnapshot,
                Sku = line.SkuSnapshot,
                Barcode = line.BarcodeSnapshot,
                LineStatus = fulfillmentLine?.LineStatus ?? line.LineStatus,
                Quantity = line.Quantity,
                PickedQuantity = picked,
                PackedQuantity = fulfillmentLine?.PackedQuantity ?? 0m,
                RemainingQuantity = remaining,
                UnitPrice = line.UnitPrice,
                LineTotal = line.LineTotalAmount,
                ImageUrl = imageLookup.GetValueOrDefault(line.ProductId),
                AltText = line.ProductNameSnapshot
            };
        }).ToList();

        return PosOnlineOrderDetailRepositoryResult.Success(new PosOnlineOrderDetailResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            ExternalReference = order.ExternalOrderReference,
            Status = displayStatus,
            StatusLabel = MapStatusLabel(displayStatus),
            OrderStatus = order.Status,
            FulfillmentStatus = fulfillment?.FulfillmentStatus ?? order.FulfillmentStatus,
            PickupStatus = pickup?.PickupStatus,
            PlacedAt = order.PlacedAt,
            UpdatedAt = order.UpdatedAt,
            SalesChannel = salesChannel,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerNameSnapshot ?? string.Empty,
            CustomerPhone = order.CustomerPhoneSnapshot,
            CustomerEmail = order.CustomerEmailSnapshot,
            CustomerClassification = null,
            CustomerNote = order.CustomerNote,
            OutletId = outletId,
            OutletName = order.ReportingOutletNameSnapshot ?? string.Empty,
            PickupNumber = pickup?.PickupNumber,
            CollectionStart = order.RequestedCollectionAt,
            CollectionEnd = order.RequestedCollectionEndAt,
            CollectionTimezone = order.CollectionTimezoneSnapshot,
            CurrencyCode = order.CurrencyCode,
            SubtotalAmount = order.SubtotalAmount,
            DiscountAmount = order.DiscountAmount,
            TaxAmount = order.TaxAmount,
            ChargeAmount = order.ChargeAmount,
            TotalAmount = order.TotalAmount,
            PaidAmount = order.PaidAmount,
            BalanceDue = order.BalanceDue,
            PaymentStatus = order.PaymentStatus,
            ItemCount = responseLines.Count,
            UnitCount = responseLines.Sum(x => x.Quantity),
            FulfillmentOrderId = fulfillment?.Id,
            FulfillmentVersion = fulfillment?.RowVersion,
            AssignedToTenantUserId = fulfillment?.AssignedToTenantUserId,
            ServerTime = serverTime,
            Lines = responseLines
        });
    }

    private async Task<string?> ValidateOutletAccessAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
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

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().Replace('-', '_').ToUpperInvariant();

    private static string DisplayStatus(
        E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder order,
        DateTimeOffset now)
    {
        if (order.Status == "CANCELLED" || order.FulfillmentStatus == "CANCELLED") return "CANCELLED";
        if (order.Status == "COMPLETED" || order.FulfillmentStatus is "FULFILLED" or "COLLECTED") return "COLLECTED";
        if (order.FulfillmentStatus is "READY" or "READY_FOR_COLLECTION" or "READY_FOR_PICKUP") return "READY";
        if ((order.RequestedCollectionEndAt ?? order.RequestedCollectionAt) < now) return "DELAYED";
        if (order.FulfillmentStatus is "PREPARING" or "PARTIALLY_FULFILLED") return "PREPARING";
        return "NEW";
    }

    private static string DisplayStatusLabel(string status) => status switch
    {
        "NEW" => "New",
        "PREPARING" => "Preparing",
        "READY" => "Ready",
        "DELAYED" => "Delayed",
        "COLLECTED" => "Collected",
        "CANCELLED" => "Cancelled",
        _ => status
    };
}
