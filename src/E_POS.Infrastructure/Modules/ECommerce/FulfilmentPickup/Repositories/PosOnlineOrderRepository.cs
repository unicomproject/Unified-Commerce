using E_POS.Application.Modules.ECommerce.FulfilmentPickup.Contracts;
using E_POS.Application.Modules.ECommerce.FulfilmentPickup.Dtos;
using E_POS.Infrastructure.Persistence;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace E_POS.Infrastructure.Modules.ECommerce.FulfilmentPickup.Repositories;

public sealed class PosOnlineOrderRepository : IPosOnlineOrderRepository
{
    private const string OrderType = "CLICK_AND_COLLECT";
    private readonly EPosDbContext _db;

    public PosOnlineOrderRepository(EPosDbContext db) => _db = db;

    public async Task<bool> CanAccessOutletAsync(
        Guid tenantId,
        Guid tenantUserId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var tenantIsActive = await _db.Tenants.AsNoTracking().AnyAsync(
            tenant => tenant.Id == tenantId && tenant.Status == TenantStatusConstants.Active,
            cancellationToken);
        if (!tenantIsActive) return false;

        var tenantUserIsActive = await _db.TenantUsers.AsNoTracking().AnyAsync(
            user => user.TenantId == tenantId && user.Id == tenantUserId &&
                    user.AccountStatus == TenantUserConstants.StatusActive,
            cancellationToken);
        if (!tenantUserIsActive) return false;

        var outletExists = await _db.Outlets.AsNoTracking().AnyAsync(
            outlet => outlet.TenantId == tenantId && outlet.Id == outletId && outlet.Status == "ACTIVE",
            cancellationToken);
        if (!outletExists) return false;

        var hasScopedAssignments = await _db.OutletUserRoles.AsNoTracking().AnyAsync(
                assignment => assignment.TenantId == tenantId && assignment.TenantUserId == tenantUserId &&
                              assignment.RevokedAt == null,
                cancellationToken) ||
            await _db.OutletUserPermissions.AsNoTracking().AnyAsync(
                assignment => assignment.TenantId == tenantId && assignment.TenantUserId == tenantUserId &&
                              assignment.RevokedAt == null,
                cancellationToken);

        if (!hasScopedAssignments) return true;

        return await _db.OutletUserRoles.AsNoTracking().AnyAsync(
                   assignment => assignment.TenantId == tenantId && assignment.TenantUserId == tenantUserId &&
                                 assignment.OutletId == outletId && assignment.RevokedAt == null,
                   cancellationToken) ||
               await _db.OutletUserPermissions.AsNoTracking().AnyAsync(
                   assignment => assignment.TenantId == tenantId && assignment.TenantUserId == tenantUserId &&
                                 assignment.OutletId == outletId && assignment.RevokedAt == null,
                   cancellationToken);
    }

    public async Task<PosOnlineOrderListDto> ListAsync(
        Guid tenantId,
        PosOnlineOrderListQuery request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var baseQuery = _db.SalesOrders.AsNoTracking().Where(order =>
            order.TenantId == tenantId &&
            order.OrderType == OrderType &&
            order.ReportingOutletId == request.OutletId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = $"%{request.Search.Trim()}%";
            baseQuery = baseQuery.Where(order =>
                EF.Functions.ILike(order.OrderNumber, search) ||
                (order.ExternalOrderReference != null && EF.Functions.ILike(order.ExternalOrderReference, search)) ||
                (order.CustomerNameSnapshot != null && EF.Functions.ILike(order.CustomerNameSnapshot, search)) ||
                (order.CustomerPhoneSnapshot != null && EF.Functions.ILike(order.CustomerPhoneSnapshot, search)) ||
                _db.FulfillmentOrders.Any(fulfillment =>
                    fulfillment.TenantId == tenantId &&
                    fulfillment.SalesOrderId == order.Id &&
                    _db.PickupOrders.Any(pickup =>
                        pickup.TenantId == tenantId &&
                        pickup.FulfillmentOrderId == fulfillment.Id &&
                        EF.Functions.ILike(pickup.PickupNumber, search))));
        }

        var cancelledQuery = baseQuery.Where(IsCancelled());
        var collectedQuery = baseQuery.Where(IsCollected());
        var readyQuery = baseQuery.Where(IsReady());
        var delayedQuery = baseQuery.Where(IsDelayed(now));
        var preparingQuery = baseQuery.Where(IsPreparing());
        var newQuery = baseQuery.Where(IsNew(now));
        var summary = new PosOnlineOrderSummaryDto(
            await newQuery.CountAsync(cancellationToken),
            await preparingQuery.CountAsync(cancellationToken),
            await readyQuery.CountAsync(cancellationToken),
            await delayedQuery.CountAsync(cancellationToken),
            await collectedQuery.CountAsync(cancellationToken),
            await cancelledQuery.CountAsync(cancellationToken));

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = NormalizeStatus(request.Status);
            baseQuery = status switch
            {
                "NEW" or "PENDING_CONFIRMATION" or "ACCEPTED" => newQuery,
                "PREPARING" => preparingQuery,
                "READY" or "READY_FOR_COLLECTION" => readyQuery,
                "DELAYED" => delayedQuery,
                "COLLECTED" or "COMPLETED" => collectedQuery,
                "CANCELLED" => cancelledQuery,
                _ => baseQuery
            };
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var sortBy = request.SortBy?.Trim().ToLowerInvariant();
        var descending = request.SortDirection?.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase) == true;
        baseQuery = (sortBy, descending) switch
        {
            ("collectiontime" or "collection_time", true) => baseQuery.OrderByDescending(x => x.RequestedCollectionAt),
            ("placedat" or "placed_at", true) => baseQuery.OrderByDescending(x => x.PlacedAt),
            ("placedat" or "placed_at", false) => baseQuery.OrderBy(x => x.PlacedAt),
            ("amount", true) => baseQuery.OrderByDescending(x => x.TotalAmount),
            ("amount", false) => baseQuery.OrderBy(x => x.TotalAmount),
            ("customer", true) => baseQuery.OrderByDescending(x => x.CustomerNameSnapshot),
            ("customer", false) => baseQuery.OrderBy(x => x.CustomerNameSnapshot),
            _ => baseQuery.OrderBy(x => x.RequestedCollectionAt).ThenBy(x => x.OrderNumber)
        };

        var rows = await baseQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(order => new
            {
                order.Id,
                order.OrderNumber,
                order.ExternalOrderReference,
                order.CustomerNameSnapshot,
                order.CustomerPhoneSnapshot,
                order.RequestedCollectionAt,
                order.RequestedCollectionEndAt,
                order.CollectionTimezoneSnapshot,
                order.Status,
                order.FulfillmentStatus,
                order.PaymentStatus,
                order.CurrencyCode,
                order.TotalAmount,
                order.PlacedAt,
                UpdatedAt = order.UpdatedAt ?? order.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var ids = rows.Select(row => row.Id).ToArray();
        var lineRows = await _db.SalesOrderLines.AsNoTracking()
            .Where(line => line.TenantId == tenantId && line.SalesOrderId.HasValue &&
                ids.Contains(line.SalesOrderId.Value) && line.LineStatus != "CANCELLED" &&
                line.Quantity > line.CancelledQuantity)
            .OrderBy(line => line.LineNumber)
            .Select(line => new
            {
                OrderId = line.SalesOrderId!.Value,
                line.ProductId,
                line.ProductVariantId,
                line.ProductNameSnapshot,
                UnitCount = line.Quantity - line.CancelledQuantity
            })
            .ToListAsync(cancellationToken);
        var lineStats = lineRows
            .GroupBy(line => line.OrderId)
            .ToDictionary(group => group.Key, group => new { ItemCount = group.Count(), UnitCount = group.Sum(x => x.UnitCount) });

        var pickupNumbers = await (
            from fulfillment in _db.FulfillmentOrders.AsNoTracking()
            join pickup in _db.PickupOrders.AsNoTracking()
                on new { fulfillment.TenantId, FulfillmentOrderId = fulfillment.Id }
                equals new { pickup.TenantId, pickup.FulfillmentOrderId }
            where fulfillment.TenantId == tenantId && ids.Contains(fulfillment.SalesOrderId)
            select new { fulfillment.SalesOrderId, pickup.PickupNumber })
            .ToDictionaryAsync(x => x.SalesOrderId, x => x.PickupNumber, cancellationToken);

        var productIds = lineRows.Select(x => x.ProductId).Distinct().ToArray();
        var imageRows = await (
            from image in _db.ProductImages.AsNoTracking()
            join asset in _db.MediaAssets.AsNoTracking()
                on new { image.TenantId, MediaAssetId = image.MediaAssetId!.Value }
                equals new { asset.TenantId, MediaAssetId = asset.Id }
            where image.TenantId == tenantId && image.MediaAssetId.HasValue &&
                  productIds.Contains(image.ProductId) && image.Status == "ACTIVE" && asset.Status == "ACTIVE"
            orderby image.IsPrimaryImage descending, image.SortOrder, image.CreatedAt
            select new { image.ProductId, image.ProductVariantId, asset.PublicUrl, image.AltText,
                image.IsPrimaryImage, image.SortOrder })
            .ToListAsync(cancellationToken);

        var items = rows.Select(row =>
        {
            var status = Status(row.Status, row.FulfillmentStatus, row.RequestedCollectionEndAt ?? row.RequestedCollectionAt, now);
            var orderLines = lineRows.Where(line => line.OrderId == row.Id).ToArray();
            var previews = orderLines.Take(4).Select(line =>
            {
                var image = imageRows
                    .Where(candidate => candidate.ProductId == line.ProductId &&
                        (candidate.ProductVariantId == line.ProductVariantId || candidate.ProductVariantId == null))
                    .OrderByDescending(candidate => candidate.ProductVariantId == line.ProductVariantId)
                    .ThenByDescending(candidate => candidate.IsPrimaryImage)
                    .ThenBy(candidate => candidate.SortOrder)
                    .FirstOrDefault();
                return new PosOnlineOrderProductPreviewDto(
                    line.ProductId, line.ProductVariantId, line.ProductNameSnapshot,
                    image?.PublicUrl, image?.AltText);
            }).ToArray();
            var stats = lineStats.GetValueOrDefault(row.Id);
            return new PosOnlineOrderListItemDto(
                row.Id, row.OrderNumber, pickupNumbers.GetValueOrDefault(row.Id) ?? row.ExternalOrderReference,
                row.CustomerNameSnapshot ?? "Walk-in Customer", row.CustomerPhoneSnapshot,
                row.RequestedCollectionAt, row.RequestedCollectionEndAt, row.CollectionTimezoneSnapshot,
                status, Label(status), row.PaymentStatus, row.CurrencyCode, row.TotalAmount,
                stats?.ItemCount ?? 0, stats?.UnitCount ?? 0m, previews,
                Math.Max(0, orderLines.Length - previews.Length), row.PlacedAt, row.UpdatedAt);
        }).ToArray();

        var pages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        return new(items, summary, request.Page, request.PageSize, totalCount, pages, now);
    }

    public async Task<PosOnlineOrderDetailDto?> GetAsync(
        Guid tenantId,
        Guid outletId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await _db.SalesOrders.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == orderId && x.OrderType == OrderType && x.ReportingOutletId == outletId)
            .Select(x => new
            {
                x.Id, x.OrderNumber, x.ExternalOrderReference, OutletId = x.ReportingOutletId!.Value,
                x.ReportingOutletNameSnapshot, x.CustomerId, x.CustomerNameSnapshot, x.CustomerPhoneSnapshot,
                x.CustomerEmailSnapshot, x.RequestedCollectionAt, x.RequestedCollectionEndAt,
                x.CollectionTimezoneSnapshot, x.Status, x.FulfillmentStatus, x.PaymentStatus, x.CurrencyCode,
                x.SubtotalAmount, x.DiscountAmount, x.TaxAmount, x.ChargeAmount, x.TotalAmount, x.PaidAmount,
                x.BalanceDue, x.CustomerNote, x.InternalNote, x.PlacedAt, UpdatedAt = x.UpdatedAt ?? x.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null) return null;

        var fulfillment = await _db.FulfillmentOrders.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == orderId)
            .Select(x => new { x.Id, x.AssignedToTenantUserId })
            .FirstOrDefaultAsync(cancellationToken);

        var fulfilmentLines = fulfillment is null
            ? new Dictionary<Guid, (decimal Picked, decimal Packed)>()
            : await _db.FulfillmentOrderLines.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == fulfillment.Id)
                .ToDictionaryAsync(x => x.SalesOrderLineId, x => new ValueTuple<decimal, decimal>(x.PickedQuantity, x.PackedQuantity), cancellationToken);

        var salesLines = await _db.SalesOrderLines.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == orderId)
            .OrderBy(x => x.LineNumber)
            .Select(x => new { x.Id, x.LineNumber, x.ProductNameSnapshot, x.VariantNameSnapshot, x.SkuSnapshot, x.BarcodeSnapshot, x.Quantity, x.UnitPrice, x.LineTotalAmount, x.LineStatus })
            .ToListAsync(cancellationToken);

        var lines = salesLines.Select(line =>
        {
            var progress = fulfilmentLines.GetValueOrDefault(line.Id);
            return new PosOnlineOrderLineDto(line.Id, line.LineNumber, line.ProductNameSnapshot,
                line.VariantNameSnapshot, line.SkuSnapshot, line.BarcodeSnapshot, line.Quantity,
                line.UnitPrice, line.LineTotalAmount, line.LineStatus, progress.Item1, progress.Item2);
        }).ToArray();

        var status = Status(
            order.Status,
            order.FulfillmentStatus,
            order.RequestedCollectionEndAt ?? order.RequestedCollectionAt,
            DateTimeOffset.MaxValue);
        return new(order.Id, order.OrderNumber, order.ExternalOrderReference, order.OutletId,
            order.ReportingOutletNameSnapshot, order.CustomerId, order.CustomerNameSnapshot ?? "Walk-in Customer",
            order.CustomerPhoneSnapshot, order.CustomerEmailSnapshot, order.RequestedCollectionAt,
            order.RequestedCollectionEndAt, order.CollectionTimezoneSnapshot, status, Label(status),
            order.PaymentStatus, order.CurrencyCode, order.SubtotalAmount, order.DiscountAmount,
            order.TaxAmount, order.ChargeAmount, order.TotalAmount, order.PaidAmount, order.BalanceDue,
            order.CustomerNote, order.InternalNote, fulfillment?.Id, fulfillment?.AssignedToTenantUserId,
            lines, order.PlacedAt, order.UpdatedAt);
    }

    public async Task<PosStartFulfillmentDto?> StartFulfillmentAsync(
        Guid tenantId,
        Guid outletId,
        Guid orderId,
        Guid tenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var order = await _db.SalesOrders
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == orderId &&
                x.OrderType == OrderType && x.ReportingOutletId == outletId, cancellationToken);
        if (order is null) return null;

        var existing = await _db.FulfillmentOrders
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.SalesOrderId == orderId, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.FulfillmentStatus, "PICKING", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This order is already in a different fulfilment state.");
            if (existing.AssignedToTenantUserId != tenantUserId)
                throw new InvalidOperationException("This order is already assigned to another staff member.");
            await transaction.CommitAsync(cancellationToken);
            return new(orderId, existing.Id, existing.FulfillmentNumber, existing.FulfillmentStatus,
                tenantUserId, existing.CreatedAt, true);
        }

        var customerStatus = order.GetClickAndCollectCustomerStatus();
        if (customerStatus == "PENDING_CONFIRMATION")
            order.UpdateClickAndCollectStatus("ACCEPTED", tenantUserId, now);
        else if (customerStatus != "ACCEPTED")
            throw new InvalidOperationException($"Fulfilment cannot be started from {customerStatus} status.");

        if (!order.FulfillmentMethodOutletId.HasValue)
            throw new InvalidOperationException("The order has no fulfilment outlet configuration.");

        var sourceLocation = await _db.InventoryLocations.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.OutletId == outletId &&
                x.Status == "ACTIVE" && x.IsSellableLocation)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new { x.Id, x.LocationCode, x.LocationName })
            .FirstOrDefaultAsync(cancellationToken);
        if (sourceLocation is null)
            throw new InvalidOperationException("No active sellable inventory location is configured for this outlet.");

        var salesLines = await _db.SalesOrderLines
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == orderId && x.LineStatus == "ACTIVE")
            .OrderBy(x => x.LineNumber)
            .ToListAsync(cancellationToken);
        if (salesLines.Count == 0)
            throw new InvalidOperationException("The order has no active lines to pick.");

        var checkoutId = ParseCheckoutId(order.ExternalOrderReference);
        var reservation = checkoutId.HasValue
            ? await _db.InventoryReservations.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.SourceReferenceId == checkoutId &&
                    x.FulfillmentOutletId == outletId && x.ReservationStatus == "CONFIRMED")
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        if (reservation is null)
            throw new InvalidOperationException("A confirmed stock reservation was not found for this order.");

        var reservedProducts = await _db.InventoryReservationLines.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.InventoryReservationId == reservation.Id &&
                x.ReservedQuantity > x.ReleasedQuantity)
            .Select(x => new { x.Id, x.ProductId, x.ProductVariantId, Available = x.ReservedQuantity - x.ReleasedQuantity })
            .ToListAsync(cancellationToken);
        foreach (var line in salesLines)
        {
            var reserved = reservedProducts
                .Where(x => x.ProductId == line.ProductId && x.ProductVariantId == line.ProductVariantId)
                .Sum(x => x.Available);
            if (reserved < line.Quantity)
                throw new InvalidOperationException($"Reserved stock is no longer sufficient for line {line.LineNumber}.");
        }

        var fulfillmentId = Guid.NewGuid();
        var fulfillment = FulfillmentOrder.StartForClickAndCollect(
            fulfillmentId, tenantId, orderId, $"FUL-{order.OrderNumber}",
            order.FulfillmentMethodOutletId.Value, sourceLocation.Id, tenantUserId, now);
        _db.FulfillmentOrders.Add(fulfillment);
        _db.FulfillmentOrderLines.AddRange(salesLines.Select(line =>
        {
            var reservationLineId = reservedProducts.First(x =>
                x.ProductId == line.ProductId && x.ProductVariantId == line.ProductVariantId).Id;
            return FulfillmentOrderLine.CreateForPicking(
                Guid.NewGuid(), tenantId, fulfillmentId, line.Id, line.Quantity, now, reservationLineId);
        }));
        _db.FulfillmentOrderEvents.Add(FulfillmentOrderEvent.Record(
            Guid.NewGuid(), tenantId, fulfillmentId, 1, "FULFILMENT_STARTED",
            null, "PICKING", tenantUserId, now));
        order.UpdateClickAndCollectStatus("PREPARING", tenantUserId, now);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(orderId, fulfillmentId, fulfillment.FulfillmentNumber, "PICKING",
                tenantUserId, now, false);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            var concurrent = await _db.FulfillmentOrders.AsNoTracking()
                .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.SalesOrderId == orderId, cancellationToken);
            if (concurrent?.AssignedToTenantUserId == tenantUserId && concurrent.FulfillmentStatus == "PICKING")
                return new(orderId, concurrent.Id, concurrent.FulfillmentNumber, concurrent.FulfillmentStatus,
                    tenantUserId, concurrent.CreatedAt, true);
            throw new InvalidOperationException("Another staff member started fulfilment for this order.");
        }
    }

    public async Task<PosPickingOrderDto?> GetPickingAsync(
        Guid tenantId,
        Guid outletId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var header = await (
            from order in _db.SalesOrders.AsNoTracking()
            join fulfillment in _db.FulfillmentOrders.AsNoTracking()
                on new { order.TenantId, SalesOrderId = order.Id }
                equals new { fulfillment.TenantId, fulfillment.SalesOrderId }
            join user in _db.TenantUsers.AsNoTracking()
                on new { fulfillment.TenantId, UserId = fulfillment.AssignedToTenantUserId!.Value }
                equals new { user.TenantId, UserId = user.Id }
            where order.TenantId == tenantId && order.Id == orderId && order.OrderType == OrderType &&
                  order.ReportingOutletId == outletId && fulfillment.AssignedToTenantUserId.HasValue
            select new
            {
                Order = order,
                Fulfillment = fulfillment,
                AssignedName = user.DisplayName ?? user.FullName
            }).SingleOrDefaultAsync(cancellationToken);
        if (header is null) return null;

        var location = header.Fulfillment.SourceInventoryLocationId.HasValue
            ? await _db.InventoryLocations.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == header.Fulfillment.SourceInventoryLocationId)
                .Select(x => new { x.LocationCode, x.LocationName })
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        var lines = await (
            from fulfillmentLine in _db.FulfillmentOrderLines.AsNoTracking()
            join salesLine in _db.SalesOrderLines.AsNoTracking()
                on new { fulfillmentLine.TenantId, Id = fulfillmentLine.SalesOrderLineId }
                equals new { salesLine.TenantId, salesLine.Id }
            where fulfillmentLine.TenantId == tenantId &&
                  fulfillmentLine.FulfillmentOrderId == header.Fulfillment.Id
            orderby salesLine.LineNumber
            select new PosPickingLineDto(
                fulfillmentLine.Id, salesLine.LineNumber, salesLine.ProductNameSnapshot,
                salesLine.VariantNameSnapshot, salesLine.SkuSnapshot, salesLine.BarcodeSnapshot,
                fulfillmentLine.RequestedQuantity, fulfillmentLine.PickedQuantity,
                fulfillmentLine.LineStatus, location == null ? null : location.LocationCode,
                location == null ? null : location.LocationName))
            .ToListAsync(cancellationToken);

        return new(header.Order.Id, header.Order.OrderNumber, header.Fulfillment.Id,
            header.Fulfillment.FulfillmentNumber, header.Fulfillment.FulfillmentStatus,
            header.Fulfillment.AssignedToTenantUserId!.Value, header.AssignedName,
            header.Order.CustomerNameSnapshot ?? "Walk-in Customer", header.Order.RequestedCollectionAt,
            lines.Count, lines.Count(x => x.PickedQuantity >= x.RequestedQuantity), lines);
    }

    public async Task<PosFulfillmentCommandDto?> PickLineAsync(Guid tenantId, Guid outletId, Guid orderId, Guid lineId, Guid userId, PosPickLineRequest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0) throw new InvalidOperationException("Pick quantity must be greater than zero.");
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var pair = await LoadCommandHeader(tenantId, outletId, orderId, cancellationToken);
        if (pair is null) return null;
        var line = await _db.FulfillmentOrderLines.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.FulfillmentOrderId == pair.Value.Fulfillment.Id && x.Id == lineId, cancellationToken)
            ?? throw new InvalidOperationException("The picking line was not found.");
        var salesLine = await _db.SalesOrderLines.AsNoTracking().SingleAsync(x => x.TenantId == tenantId && x.Id == line.SalesOrderLineId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(salesLine.BarcodeSnapshot) && !string.Equals(salesLine.BarcodeSnapshot, request.Barcode?.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The barcode does not match the selected product variant.");
        line.Pick(request.Quantity, userId, now);
        var lines = await _db.FulfillmentOrderLines.Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == pair.Value.Fulfillment.Id).ToListAsync(cancellationToken);
        if (lines.All(x => x.PickedQuantity == x.RequestedQuantity)) pair.Value.Fulfillment.MarkPicked(userId, now);
        await AddEvent(tenantId, pair.Value.Fulfillment.Id, "ITEM_PICKED", pair.Value.Fulfillment.FulfillmentStatus, userId, now, $"Line {lineId} quantity {request.Quantity}", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        return Result(pair.Value.Order.Id, pair.Value.Fulfillment, lines, null, now);
    }

    public async Task<PosFulfillmentCommandDto?> ReportIssueAsync(Guid tenantId, Guid outletId, Guid orderId, Guid lineId, Guid userId, PosReportPickingIssueRequest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("An issue reason is required.");
        var pair = await LoadCommandHeader(tenantId, outletId, orderId, cancellationToken); if (pair is null) return null;
        var exists = await _db.FulfillmentOrderLines.AnyAsync(x => x.TenantId == tenantId && x.FulfillmentOrderId == pair.Value.Fulfillment.Id && x.Id == lineId, cancellationToken);
        if (!exists) throw new InvalidOperationException("The picking line was not found.");
        await AddEvent(tenantId, pair.Value.Fulfillment.Id, "PICKING_ISSUE_REPORTED", pair.Value.Fulfillment.FulfillmentStatus, userId, now, $"{request.Reason.Trim()}: {request.Note?.Trim()}", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        var lines = await _db.FulfillmentOrderLines.AsNoTracking().Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == pair.Value.Fulfillment.Id).ToListAsync(cancellationToken);
        return Result(orderId, pair.Value.Fulfillment, lines, null, now);
    }

    public async Task<PosFulfillmentCommandDto?> PackAsync(Guid tenantId, Guid outletId, Guid orderId, Guid userId, PosPackOrderRequest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (request.PackingNote?.Length > 200) throw new InvalidOperationException("Packing note cannot exceed 200 characters.");
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var pair = await LoadCommandHeader(tenantId, outletId, orderId, cancellationToken); if (pair is null) return null;
        var lines = await _db.FulfillmentOrderLines.Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == pair.Value.Fulfillment.Id).ToListAsync(cancellationToken);
        if (lines.Count == 0 || lines.Any(x => x.PickedQuantity != x.RequestedQuantity)) throw new InvalidOperationException("All required quantities must be picked before packing.");
        var existing = await _db.FulfillmentPackages.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.FulfillmentOrderId == pair.Value.Fulfillment.Id, cancellationToken);
        if (pair.Value.Fulfillment.FulfillmentStatus == "PACKED" && existing is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return Result(orderId, pair.Value.Fulfillment, lines, existing.PackageNumber, now);
        }
        var package = existing ?? FulfillmentPackage.Create(Guid.NewGuid(), tenantId, pair.Value.Fulfillment.Id, $"PKG-{pair.Value.Fulfillment.FulfillmentNumber}-01", userId, request.PackingNote, now);
        if (existing is null)
        {
            _db.FulfillmentPackages.Add(package);
            _db.FulfillmentPackageLines.AddRange(lines.Select(x => FulfillmentPackageLine.Create(Guid.NewGuid(), tenantId, package.Id, x.Id, x.PickedQuantity, now)));
        }
        foreach (var line in lines) line.Pack(userId, now);
        pair.Value.Fulfillment.MarkPacked(userId, now);
        await AddEvent(tenantId, pair.Value.Fulfillment.Id, "ORDER_PACKED", "PACKED", userId, now, request.PackingNote, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        return Result(orderId, pair.Value.Fulfillment, lines, package.PackageNumber, now);
    }

    public async Task<PosFulfillmentCommandDto?> MarkReadyAsync(Guid tenantId, Guid outletId, Guid orderId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var pair = await LoadCommandHeader(tenantId, outletId, orderId, cancellationToken); if (pair is null) return null;
        var packages = await _db.FulfillmentPackages.Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == pair.Value.Fulfillment.Id).ToListAsync(cancellationToken);
        if (packages.Count == 0 || packages.Any(x => x.PackageStatus != "PACKED" && x.PackageStatus != "READY")) throw new InvalidOperationException("A packed package is required before marking the order ready.");
        if (pair.Value.Fulfillment.FulfillmentStatus == "READY" && packages.All(x => x.PackageStatus == "READY"))
        {
            await transaction.CommitAsync(cancellationToken);
            var readyLines = await _db.FulfillmentOrderLines.AsNoTracking().Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == pair.Value.Fulfillment.Id).ToListAsync(cancellationToken);
            return Result(orderId, pair.Value.Fulfillment, readyLines, packages[0].PackageNumber, now);
        }
        foreach (var package in packages) package.MarkReady(now);
        pair.Value.Fulfillment.MarkReady(userId, now);
        pair.Value.Order.UpdateClickAndCollectStatus("READY_FOR_COLLECTION", userId, now);
        var pickup = await _db.PickupOrders.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.FulfillmentOrderId == pair.Value.Fulfillment.Id, cancellationToken);
        if (pickup is null)
            _db.PickupOrders.Add(PickupOrder.CreateReady(Guid.NewGuid(), tenantId, pair.Value.Fulfillment.Id, $"PU-{pair.Value.Order.OrderNumber}", pair.Value.Order.CustomerNameSnapshot ?? "Walk-in Customer", pair.Value.Order.CustomerPhoneSnapshot, pair.Value.Order.CustomerEmailSnapshot, now));
        else pickup.MarkReady(now);
        await AddEvent(tenantId, pair.Value.Fulfillment.Id, "READY_FOR_COLLECTION", "READY", userId, now, null, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        var lines = await _db.FulfillmentOrderLines.AsNoTracking().Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == pair.Value.Fulfillment.Id).ToListAsync(cancellationToken);
        return Result(orderId, pair.Value.Fulfillment, lines, packages[0].PackageNumber, now);
    }

    private async Task<(E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder Order, FulfillmentOrder Fulfillment)?> LoadCommandHeader(Guid tenantId, Guid outletId, Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _db.SalesOrders.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == orderId && x.OrderType == OrderType && x.ReportingOutletId == outletId, cancellationToken);
        if (order is null) return null;
        var fulfillment = await _db.FulfillmentOrders.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.SalesOrderId == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Fulfilment has not been started.");
        return (order, fulfillment);
    }

    private async Task AddEvent(Guid tenantId, Guid fulfillmentId, string eventType, string status, Guid userId, DateTimeOffset now, string? note, CancellationToken cancellationToken)
    {
        var sequence = await _db.FulfillmentOrderEvents.Where(x => x.TenantId == tenantId && x.FulfillmentOrderId == fulfillmentId).MaxAsync(x => (int?)x.SequenceNumber, cancellationToken) ?? 0;
        _db.FulfillmentOrderEvents.Add(FulfillmentOrderEvent.Record(Guid.NewGuid(), tenantId, fulfillmentId, sequence + 1, eventType, null, status, userId, now, note));
    }

    private static PosFulfillmentCommandDto Result(Guid orderId, FulfillmentOrder fulfillment, IReadOnlyCollection<FulfillmentOrderLine> lines, string? packageNumber, DateTimeOffset now) =>
        new(orderId, fulfillment.Id, fulfillment.FulfillmentStatus, lines.Count, lines.Count(x => x.PickedQuantity == x.RequestedQuantity), packageNumber, now);

    private static Guid? ParseCheckoutId(string? externalReference)
    {
        if (string.IsNullOrWhiteSpace(externalReference)) return null;
        var parts = externalReference.Split(':', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && parts[0].Equals("CHECKOUT", StringComparison.OrdinalIgnoreCase) &&
               Guid.TryParse(parts[1], out var checkoutId)
            ? checkoutId
            : null;
    }

    private static string NormalizeStatus(string value) => value.Trim().Replace('-', '_').ToUpperInvariant();

    private static Expression<Func<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder, bool>> IsCancelled() =>
        order => order.Status == "CANCELLED" || order.FulfillmentStatus == "CANCELLED";

    private static Expression<Func<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder, bool>> IsCollected() =>
        order => order.Status == "COMPLETED" || order.FulfillmentStatus == "COLLECTED" ||
                 order.FulfillmentStatus == "FULFILLED";

    private static Expression<Func<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder, bool>> IsReady() =>
        order => order.Status != "CANCELLED" && order.FulfillmentStatus != "CANCELLED" &&
                 order.Status != "COMPLETED" && order.FulfillmentStatus != "COLLECTED" &&
                 order.FulfillmentStatus != "FULFILLED" &&
                 (order.FulfillmentStatus == "READY" || order.FulfillmentStatus == "READY_FOR_COLLECTION");

    private static Expression<Func<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder, bool>> IsDelayed(DateTimeOffset now) =>
        order => order.Status != "CANCELLED" && order.FulfillmentStatus != "CANCELLED" &&
                 order.Status != "COMPLETED" && order.FulfillmentStatus != "COLLECTED" &&
                 order.FulfillmentStatus != "FULFILLED" && order.FulfillmentStatus != "READY" &&
                 order.FulfillmentStatus != "READY_FOR_COLLECTION" &&
                 (order.RequestedCollectionEndAt ?? order.RequestedCollectionAt) < now;

    private static Expression<Func<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder, bool>> IsPreparing() =>
        order => order.Status != "CANCELLED" && order.FulfillmentStatus != "CANCELLED" &&
                 order.Status != "COMPLETED" && order.FulfillmentStatus != "COLLECTED" &&
                 order.FulfillmentStatus != "FULFILLED" && order.FulfillmentStatus != "READY" &&
                 order.FulfillmentStatus != "READY_FOR_COLLECTION" && order.FulfillmentStatus == "PREPARING";

    private static Expression<Func<E_POS.Domain.Modules.Tenant.Orders.Entities.SalesOrder, bool>> IsNew(DateTimeOffset now) =>
        order => order.Status != "CANCELLED" && order.FulfillmentStatus != "CANCELLED" &&
                 order.Status != "COMPLETED" && order.FulfillmentStatus != "COLLECTED" &&
                 order.FulfillmentStatus != "FULFILLED" && order.FulfillmentStatus != "READY" &&
                 order.FulfillmentStatus != "READY_FOR_COLLECTION" && order.FulfillmentStatus != "PREPARING" &&
                 !((order.RequestedCollectionEndAt ?? order.RequestedCollectionAt) < now);

    private static string Status(
        string orderStatus,
        string fulfillmentStatus,
        DateTimeOffset? collectionDeadline,
        DateTimeOffset now)
    {
        if (orderStatus.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase) || fulfillmentStatus.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase)) return "CANCELLED";
        if (orderStatus.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase) || fulfillmentStatus is "FULFILLED" or "COLLECTED") return "COLLECTED";
        if (fulfillmentStatus is "READY" or "READY_FOR_COLLECTION") return "READY";
        if (collectionDeadline < now) return "DELAYED";
        if (fulfillmentStatus.Equals("PREPARING", StringComparison.OrdinalIgnoreCase)) return "PREPARING";
        return "NEW";
    }

    private static string Label(string status) => status switch
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
