using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;

public sealed partial class PosOnlineOrderPickingRepository
{
    public async Task<PosOnlineOrderPickingRepositoryResult> GetAsync(
        Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId,
        DateTimeOffset serverTime, CancellationToken cancellationToken)
    {
        var accessError = await ValidateAccessAsync(tenantId, tenantUserId, outletId, cancellationToken);
        if (accessError is not null)
            return PosOnlineOrderPickingRepositoryResult.Failure(accessError);

        var aggregate = await LoadAggregateAsync(
            tenantId, outletId, orderId, tracked: false, cancellationToken);
        if (aggregate is null)
            return PosOnlineOrderPickingRepositoryResult.Failure("online_orders.not_found");
        var pickup = await DbContext.PickupOrders.AsNoTracking().SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.FulfillmentOrderId == aggregate.Fulfillment.Id,
            cancellationToken);
        if (aggregate.Fulfillment.FulfillmentStatus is not ("PICKING" or "PACKED" or "READY") ||
            aggregate.Order.Status is "CANCELLED" or "COMPLETED" or "COLLECTED" or "FULFILLED" or "VOIDED" ||
            aggregate.Order.CompletedAt.HasValue || aggregate.Order.CancelledAt.HasValue ||
            pickup?.CollectedAt is not null ||
            pickup?.PickupStatus is "COLLECTED" or "CANCELLED" or "EXPIRED" ||
            (aggregate.Fulfillment.FulfillmentStatus == "READY" &&
             !ReadyForCollectionPolicy.IsReady(aggregate.Order, aggregate.Fulfillment, pickup)))
            return PosOnlineOrderPickingRepositoryResult.Failure("online_orders.invalid_state");

        // Load issue payloads in a separate query, then evaluate Contains in memory.
        // jsonb columns must not use ToLower()/Contains in SQL (Postgres: lower(jsonb) fails).
        var rows = await (
            from fulfillmentLine in DbContext.FulfillmentOrderLines.AsNoTracking()
            join salesLine in DbContext.SalesOrderLines.AsNoTracking()
                on new { fulfillmentLine.TenantId, Id = fulfillmentLine.SalesOrderLineId }
                equals new { salesLine.TenantId, Id = salesLine.Id }
            where fulfillmentLine.TenantId == tenantId &&
                  fulfillmentLine.FulfillmentOrderId == aggregate.Fulfillment.Id &&
                  salesLine.SalesOrderId == orderId && salesLine.LineStatus != "CANCELLED"
            orderby salesLine.LineNumber
            select new
            {
                FulfillmentLine = fulfillmentLine,
                SalesLine = salesLine
            })
            .ToListAsync(cancellationToken);
        var issuePayloads = await DbContext.FulfillmentOrderEvents.AsNoTracking()
            .Where(e =>
                e.TenantId == tenantId &&
                e.FulfillmentOrderId == aggregate.Fulfillment.Id &&
                e.EventType == IssueReportedEvent &&
                e.EventPayloadJson != null)
            .Select(e => e.EventPayloadJson!)
            .ToListAsync(cancellationToken);

        var imageLookup = await BuildImageLookupAsync(
            tenantId, rows.Select(x => x.SalesLine.ProductId).Distinct().ToList(), cancellationToken);
        var location = aggregate.Fulfillment.SourceInventoryLocationId.HasValue
            ? await DbContext.InventoryLocations.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.OutletId == outletId &&
                            x.Id == aggregate.Fulfillment.SourceInventoryLocationId.Value)
                .Select(x => new { x.LocationCode, x.LocationName })
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        var assignedName = aggregate.Fulfillment.AssignedToTenantUserId.HasValue
            ? await DbContext.TenantUsers.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == aggregate.Fulfillment.AssignedToTenantUserId.Value)
                .Select(x => x.DisplayName ?? x.FullName)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        var notesDescending = await (
            from noteEvent in DbContext.FulfillmentOrderEvents.AsNoTracking()
            join actor in DbContext.TenantUsers.AsNoTracking()
                on new { noteEvent.TenantId, Id = noteEvent.EventByTenantUserId!.Value }
                equals new { actor.TenantId, Id = actor.Id }
            where noteEvent.TenantId == tenantId &&
                  noteEvent.FulfillmentOrderId == aggregate.Fulfillment.Id &&
                  noteEvent.EventType == PickingNoteAddedEvent &&
                  noteEvent.EventNote != null &&
                  noteEvent.EventByTenantUserId != null
            orderby noteEvent.SequenceNumber descending
            select new PosOnlineOrderPickingNoteResponse
            {
                Id = noteEvent.Id,
                Note = noteEvent.EventNote!,
                CreatedAt = noteEvent.EventAt,
                CreatedByTenantUserId = actor.Id,
                CreatedByDisplayName = actor.DisplayName ?? actor.FullName
            })
            .Take(PickingNoteHistoryLimit)
            .ToListAsync(cancellationToken);
        notesDescending.Reverse();

        var lines = rows.Select(row =>
        {
            var requested = row.FulfillmentLine.RequestedQuantity;
            var remaining = Math.Max(requested - row.FulfillmentLine.CancelledQuantity -
                                     row.FulfillmentLine.PickedQuantity, 0m);
            var lineIdText = row.FulfillmentLine.Id.ToString();
            var hasReportedIssue = issuePayloads.Any(payload =>
                payload.Contains(lineIdText, StringComparison.OrdinalIgnoreCase));
            return new PosOnlineOrderPickingLineResponse
            {
                Id = row.FulfillmentLine.Id,
                SalesOrderLineId = row.SalesLine.Id,
                ProductId = row.SalesLine.ProductId,
                ProductVariantId = row.SalesLine.ProductVariantId,
                LineNumber = row.SalesLine.LineNumber,
                ProductName = row.SalesLine.ProductNameSnapshot,
                VariantName = row.SalesLine.VariantNameSnapshot,
                Sku = row.SalesLine.SkuSnapshot,
                Barcode = row.SalesLine.BarcodeSnapshot,
                ImageUrl = imageLookup.GetValueOrDefault(row.SalesLine.ProductId),
                AltText = row.SalesLine.ProductNameSnapshot,
                LocationCode = location?.LocationCode,
                LocationName = location?.LocationName,
                RequestedQuantity = requested,
                PickedQuantity = row.FulfillmentLine.PickedQuantity,
                PackedQuantity = row.FulfillmentLine.PackedQuantity,
                CancelledQuantity = row.FulfillmentLine.CancelledQuantity,
                RemainingQuantity = remaining,
                Status = row.FulfillmentLine.LineStatus,
                HasReportedIssue = hasReportedIssue
            };
        }).ToList();

        var pickedLines = lines.Count(x => x.RemainingQuantity == 0);
        var totalUnits = lines.Sum(x => Math.Max(x.RequestedQuantity - x.CancelledQuantity, 0m));
        var pickedUnits = lines.Sum(x => x.PickedQuantity);
        var canPack = CanPackPickingLines(
            aggregate.Fulfillment.FulfillmentStatus, lines.Select(x => x.RemainingQuantity));
        string? readyNotificationStatus = null;
        if (aggregate.Fulfillment.FulfillmentStatus == "READY")
        {
            var eventNumber = $"ECOM-ORDER-READY-{orderId:N}".ToUpperInvariant();
            readyNotificationStatus = await (
                from notification in DbContext.NotificationEvents.AsNoTracking()
                join message in DbContext.NotificationMessages.AsNoTracking()
                    on notification.Id equals message.NotificationEventId
                where notification.TenantId == tenantId && notification.EventNumber == eventNumber &&
                      message.TenantId == tenantId && message.CustomerId == aggregate.Order.CustomerId &&
                      message.ChannelType == "IN_APP"
                select message.MessageStatus).FirstOrDefaultAsync(cancellationToken) ?? "NOT_SENT";
        }
        return PosOnlineOrderPickingRepositoryResult.QuerySuccess(new PosOnlineOrderPickingResponse
        {
            SalesOrderStatus = aggregate.Order.Status,
            FulfillmentStatus = aggregate.Fulfillment.FulfillmentStatus,
            PickupStatus = pickup?.PickupStatus,
            ReadyAt = aggregate.Fulfillment.ReadyAt,
            CollectedAt = pickup?.CollectedAt,
            CollectionEndAt = aggregate.Order.RequestedCollectionEndAt,
            CollectionTimezone = aggregate.Order.CollectionTimezoneSnapshot,
            IssueCount = lines.Count(x => x.HasReportedIssue),
            ReadyNotificationStatus = readyNotificationStatus,
            OrderId = aggregate.Order.Id,
            OrderNumber = aggregate.Order.OrderNumber,
            FulfillmentOrderId = aggregate.Fulfillment.Id,
            FulfillmentNumber = aggregate.Fulfillment.FulfillmentNumber,
            Status = aggregate.Fulfillment.FulfillmentStatus,
            AssignedToTenantUserId = aggregate.Fulfillment.AssignedToTenantUserId,
            AssignedToName = assignedName ?? string.Empty,
            CustomerName = aggregate.Order.CustomerNameSnapshot ?? string.Empty,
            CollectionAt = aggregate.Order.RequestedCollectionAt,
            OutletId = outletId,
            OutletName = aggregate.Order.ReportingOutletNameSnapshot ?? string.Empty,
            TotalLines = lines.Count,
            PickedLines = pickedLines,
            TotalUnits = totalUnits,
            PickedUnits = pickedUnits,
            RemainingUnits = Math.Max(totalUnits - pickedUnits, 0m),
            CanPack = canPack,
            FulfillmentVersion = aggregate.Fulfillment.RowVersion,
            ServerTime = serverTime,
            Lines = lines,
            Notes = notesDescending
        });
    }
}
