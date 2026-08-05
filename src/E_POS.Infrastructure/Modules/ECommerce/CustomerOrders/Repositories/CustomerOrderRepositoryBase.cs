using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.Shared.Media;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;

public abstract class CustomerOrderRepositoryBase
{
    protected const string ActiveStatus = "ACTIVE";
    protected const string ClickAndCollectOrderType = "CLICK_AND_COLLECT";

    protected CustomerOrderRepositoryBase(EPosDbContext dbContext)
    {
        DbContext = dbContext;
    }

    protected EPosDbContext DbContext { get; }

    protected static IQueryable<SalesOrder> ApplyStatusFilter(
        IQueryable<SalesOrder> query,
        string? normalizedStatus) => normalizedStatus switch
        {
            "PENDING_CONFIRMATION" => query.Where(x =>
                x.Status == "PLACED" ||
                (x.Status == "CONFIRMED" && x.FulfillmentStatus == "PENDING")),
            "ACCEPTED" => query.Where(x =>
                x.Status == "ACCEPTED" && x.FulfillmentStatus == "ACCEPTED"),
            "PREPARING" => query.Where(x =>
                (x.Status == "ACCEPTED" && x.FulfillmentStatus == "PREPARING") ||
                (x.Status == "CONFIRMED" && x.FulfillmentStatus == "PARTIALLY_FULFILLED")),
            "READY_FOR_COLLECTION" => query.Where(x =>
                (x.Status == "ACCEPTED" && x.FulfillmentStatus == "READY_FOR_COLLECTION") ||
                (x.Status == "CONFIRMED" && x.FulfillmentStatus == "READY_FOR_PICKUP") ||
                x.FulfillmentStatus == "READY"),
            "COMPLETED" => query.Where(x =>
                x.Status == "COMPLETED" ||
                x.FulfillmentStatus == "FULFILLED" ||
                x.FulfillmentStatus == "COLLECTED"),
            "CANCELLED" => query.Where(x =>
                x.Status == "CANCELLED" ||
                x.FulfillmentStatus == "CANCELLED"),
            _ => query
        };

    protected async Task<Dictionary<Guid, string?>> BuildImageLookupAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return [];

        var images = await (from image in DbContext.ProductImages.AsNoTracking()
                            join mediaAsset in DbContext.Set<MediaAsset>().AsNoTracking()
                                on new { image.TenantId, MediaAssetId = image.MediaAssetId }
                                equals new { mediaAsset.TenantId, MediaAssetId = (Guid?)mediaAsset.Id } into mediaAssets
                            from mediaAsset in mediaAssets.DefaultIfEmpty()
                            where image.TenantId == tenantId &&
                                  productIds.Contains(image.ProductId) &&
                                  image.Status == ActiveStatus
                            orderby image.IsPrimaryImage descending, image.SortOrder, image.Id
                            select new
                            {
                                Image = image,
                                MediaStatus = mediaAsset == null ? null : mediaAsset.Status,
                                MediaPublicUrl = mediaAsset == null ? null : mediaAsset.PublicUrl
                            })
            .ToListAsync(cancellationToken);

        return images
            .Where(x => x.MediaStatus == ActiveStatus && !string.IsNullOrWhiteSpace(x.MediaPublicUrl))
            .GroupBy(x => x.Image.ProductId)
            .ToDictionary(
                x => x.Key,
                x => (string?)x.First().MediaPublicUrl);
    }

    protected static CustomerOrderSummaryReadModel BuildSummary(
        SalesOrder order,
        IReadOnlyDictionary<Guid, List<SalesOrderLine>> linesByOrder,
        IReadOnlyDictionary<Guid, string?> imageLookup)
    {
        linesByOrder.TryGetValue(order.Id, out var lines);
        lines ??= [];

        var status = MapUiStatus(order);
        var thumbnails = lines
            .Select(line => imageLookup.GetValueOrDefault(line.ProductId))
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .Cast<string>()
            .ToList();

        return new CustomerOrderSummaryReadModel
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            DisplayOrderNumber = order.OrderNumber,
            Status = status,
            StatusLabel = MapStatusLabel(status),
            PlacedAt = order.PlacedAt,
            RequestedCollectionAt = order.RequestedCollectionAt,
            RequestedCollectionEndAt = order.RequestedCollectionEndAt,
            CollectionTimezone = order.CollectionTimezoneSnapshot,
            OutletId = order.ReportingOutletId,
            OutletName = order.ReportingOutletNameSnapshot,
            ItemCount = ToWholeItemCount(lines.Sum(x => x.Quantity)),
            GrandTotal = order.TotalAmount,
            CurrencyCode = order.CurrencyCode,
            PaymentStatus = order.PaymentStatus,
            PaymentLabel = MapPaymentLabel(order.PaymentStatus),
            ThumbnailUrls = thumbnails
        };
    }

    protected static CustomerOrderDetailReadModel BuildDetail(
        SalesOrder order,
        IReadOnlyList<SalesOrderLine> lines,
        IReadOnlyDictionary<Guid, string?> imageLookup,
        IReadOnlyList<SalesOrderStatusHistory> statusHistory)
    {
        var status = MapUiStatus(order);
        var canShowQr = CanShowCollectionQr(status);

        return new CustomerOrderDetailReadModel
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            DisplayOrderNumber = order.OrderNumber,
            Status = status,
            StatusLabel = MapStatusLabel(status),
            StatusMessage = MapStatusMessage(status),
            CanShowCollectionQr = canShowQr,
            CollectionQr = canShowQr ? BuildCollectionQr(order) : null,
            PlacedAt = order.PlacedAt,
            RequestedCollectionAt = order.RequestedCollectionAt,
            RequestedCollectionEndAt = order.RequestedCollectionEndAt,
            CollectionTimezone = order.CollectionTimezoneSnapshot,
            OutletId = order.ReportingOutletId,
            OutletName = order.ReportingOutletNameSnapshot,
            PaymentStatus = order.PaymentStatus,
            PaymentLabel = MapPaymentLabel(order.PaymentStatus),
            CurrencyCode = order.CurrencyCode,
            ItemCount = ToWholeItemCount(lines.Sum(x => x.Quantity)),
            SubtotalAmount = order.SubtotalAmount,
            DiscountAmount = order.DiscountAmount,
            TaxAmount = order.TaxAmount,
            CollectionChargeAmount = order.ChargeAmount,
            GrandTotal = order.TotalAmount,
            IsTaxInclusive = order.IsTaxInclusive,
            TimelineSteps = BuildTimeline(order, status, statusHistory),
            AvailableActions = BuildAvailableActions(status),
            Items = lines.Select(line => BuildDetailItem(line, imageLookup)).ToList()
        };
    }

    protected static IReadOnlyList<CustomerOrderTimelineStepReadModel> BuildTimeline(
        SalesOrder order,
        string status,
        IReadOnlyList<SalesOrderStatusHistory> history)
    {
        if (status == "CANCELLED")
        {
            return
            [
                new CustomerOrderTimelineStepReadModel
                {
                    Code = "ORDER_SUBMITTED",
                    Label = "Order Submitted",
                    Description = "Your order has been placed successfully.",
                    State = "COMPLETED",
                    OccurredAt = order.PlacedAt,
                    Icon = "lucideClipboardList",
                    BadgeLabel = null
                },
                new CustomerOrderTimelineStepReadModel
                {
                    Code = "CANCELLED",
                    Label = "Order Cancelled",
                    Description = order.CancellationReason ?? "This order was cancelled.",
                    State = "ERROR",
                    OccurredAt = order.CancelledAt ?? FirstChangedAt(history, "STATUS", "CANCELLED") ?? order.UpdatedAt,
                    Icon = "lucideXCircle",
                    BadgeLabel = "Cancelled"
                }
            ];
        }

        return
        [
            new CustomerOrderTimelineStepReadModel
            {
                Code = "ORDER_SUBMITTED",
                Label = "Order Submitted",
                Description = "Your order has been placed successfully.",
                State = "COMPLETED",
                OccurredAt = order.PlacedAt,
                Icon = "lucideClipboardList",
                BadgeLabel = null
            },
            new CustomerOrderTimelineStepReadModel
            {
                Code = "ORDER_CONFIRMED",
                Label = "Order Confirmed",
                Description = "The store has accepted your order.",
                State = status == "PENDING_CONFIRMATION" ? "CURRENT" : "COMPLETED",
                OccurredAt = FirstChangedAt(history, "FULFILLMENT_STATUS", "ACCEPTED"),
                Icon = "lucideCheckCircle2",
                BadgeLabel = status == "PENDING_CONFIRMATION" ? "Waiting for Store" : null
            },
            new CustomerOrderTimelineStepReadModel
            {
                Code = "PREPARING",
                Label = "Preparing Your Order",
                Description = "We are preparing your items.",
                State = status switch
                {
                    "PENDING_CONFIRMATION" => "PENDING",
                    "ACCEPTED" or "PREPARING" => "CURRENT",
                    "READY_FOR_COLLECTION" or "COMPLETED" => "COMPLETED",
                    _ => "PENDING"
                },
                OccurredAt = FirstChangedAt(history, "FULFILLMENT_STATUS", "PREPARING"),
                Icon = "lucideBox",
                BadgeLabel = status is "ACCEPTED" or "PREPARING" ? "In Progress" : null
            },
            new CustomerOrderTimelineStepReadModel
            {
                Code = "READY_FOR_COLLECTION",
                Label = "Ready for Collection",
                Description = "Your order is ready to collect at {order.ReportingOutletNameSnapshot}.",
                State = status switch
                {
                    "READY_FOR_COLLECTION" => "CURRENT",
                    "COMPLETED" => "COMPLETED",
                    _ => "PENDING"
                },
                OccurredAt = FirstChangedAt(history, "FULFILLMENT_STATUS", "READY_FOR_COLLECTION"),
                Icon = "lucideShoppingBag",
                BadgeLabel = status == "READY_FOR_COLLECTION" ? "In Progress" : null
            },
            new CustomerOrderTimelineStepReadModel
            {
                Code = "ORDER_COLLECTED",
                Label = "Order Collected",
                Description = "Your order has been collected.",
                State = status == "COMPLETED" ? "COMPLETED" : "PENDING",
                OccurredAt = order.CompletedAt ?? FirstChangedAt(history, "FULFILLMENT_STATUS", "COLLECTED"),
                Icon = "lucidePackageCheck"
            }
        ];
    }

    protected static IReadOnlyList<string> BuildAvailableActions(string status)
    {
        var actions = new List<string>();
        if (status is not "COMPLETED" and not "CANCELLED")
            actions.Add("TRACK");

        if (status is "PENDING_CONFIRMATION" or "ACCEPTED")
            actions.Add("CANCEL");

        actions.Add("NEED_HELP");
        return actions;
    }

    protected async Task AddStatusHistoryAsync(
        Guid tenantId,
        SalesOrder order,
        string oldOrderStatus,
        string oldFulfillmentStatus,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sequence = await DbContext.SalesOrderStatusHistory
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == order.Id)
            .MaxAsync(x => (int?)x.SequenceNumber, cancellationToken) ?? 0;

        if (!Is(oldOrderStatus, order.Status))
        {
            DbContext.SalesOrderStatusHistory.Add(SalesOrderStatusHistory.Create(
                Guid.NewGuid(),
                tenantId,
                order.Id,
                ++sequence,
                "ORDER_STATUS",
                oldOrderStatus,
                order.Status,
                null,
                now,
                "Customer cancelled order"));
        }

        if (!Is(oldFulfillmentStatus, order.FulfillmentStatus))
        {
            DbContext.SalesOrderStatusHistory.Add(SalesOrderStatusHistory.Create(
                Guid.NewGuid(),
                tenantId,
                order.Id,
                ++sequence,
                "FULFILLMENT_STATUS",
                oldFulfillmentStatus,
                order.FulfillmentStatus,
                null,
                now,
                "Customer cancelled order"));
        }
    }

    protected static CustomerOrderDetailItemReadModel BuildDetailItem(
        SalesOrderLine line,
        IReadOnlyDictionary<Guid, string?> imageLookup) =>
        new()
        {
            Id = line.Id,
            ProductId = line.ProductId,
            ProductVariantId = line.ProductVariantId,
            ProductName = line.ProductNameSnapshot,
            VariantName = line.VariantNameSnapshot,
            Sku = line.SkuSnapshot,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            LineTotal = line.LineTotalAmount,
            ImageUrl = imageLookup.GetValueOrDefault(line.ProductId)
        };

    protected static string MapUiStatus(SalesOrder order)
    {
        if (Is(order.Status, "CANCELLED") || Is(order.FulfillmentStatus, "CANCELLED"))
            return "CANCELLED";

        if (Is(order.Status, "COMPLETED") ||
            Is(order.FulfillmentStatus, "FULFILLED") ||
            Is(order.FulfillmentStatus, "COLLECTED"))
            return "COMPLETED";

        if ((Is(order.Status, "ACCEPTED") && Is(order.FulfillmentStatus, "READY_FOR_COLLECTION")) ||
            (Is(order.Status, "CONFIRMED") && Is(order.FulfillmentStatus, "READY_FOR_PICKUP")) ||
            Is(order.FulfillmentStatus, "READY"))
            return "READY_FOR_COLLECTION";

        if ((Is(order.Status, "ACCEPTED") && Is(order.FulfillmentStatus, "PREPARING")) ||
            (Is(order.Status, "CONFIRMED") && Is(order.FulfillmentStatus, "PARTIALLY_FULFILLED")))
            return "PREPARING";

        if (Is(order.Status, "ACCEPTED") || Is(order.FulfillmentStatus, "ACCEPTED"))
            return "ACCEPTED";

        return "PENDING_CONFIRMATION";
    }

    protected static string MapStatusLabel(string status) => status switch
    {
        "PENDING_CONFIRMATION" => "Pending Confirmation",
        "ACCEPTED" => "Accepted",
        "PREPARING" => "Preparing",
        "READY_FOR_COLLECTION" => "Ready for Collection",
        "COMPLETED" => "Completed",
        "CANCELLED" => "Cancelled",
        _ => status
    };

    protected static string MapStatusMessage(string status) => status switch
    {
        "PENDING_CONFIRMATION" => "Your order is waiting for outlet acceptance. QR code will appear when the order is ready for collection.",
        "ACCEPTED" => "Your order has been accepted. QR code will appear when the order is ready for collection.",
        "PREPARING" => "Your order is being prepared by the outlet. QR code will appear when it is ready for collection.",
        "READY_FOR_COLLECTION" => "Your order is ready for collection.",
        "COMPLETED" => "Your order has been completed.",
        "CANCELLED" => "Your order has been cancelled.",
        _ => string.Empty
    };

    protected static string MapPaymentLabel(string paymentStatus) => paymentStatus switch
    {
        "UNPAID" => "Pay at Pickup",
        "PAID" => "Paid",
        "PARTIALLY_REFUNDED" => "Partially Refunded",
        "REFUNDED" => "Refunded",
        _ => paymentStatus
    };

    protected static int ToWholeItemCount(decimal quantity) =>
        (int)decimal.Round(quantity, 0, MidpointRounding.AwayFromZero);

    protected static int CalculateTotalPages(int totalCount, int pageSize) =>
        totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

    protected static DateTimeOffset? FirstChangedAt(
        IReadOnlyList<SalesOrderStatusHistory> history,
        string statusType,
        string newStatus) =>
        history.FirstOrDefault(x =>
            Is(x.StatusType, statusType) &&
            Is(x.NewStatus, newStatus))?.ChangedAt;

    protected static bool CanShowCollectionQr(string status) =>
        status is "READY_FOR_COLLECTION";

    protected static string BuildCollectionQr(SalesOrder order) =>
        $"CLICK_COLLECT:{order.TenantId:N}:{order.Id:N}:{order.OrderNumber}";

    protected static bool Is(string value, string expected) =>
        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
}
