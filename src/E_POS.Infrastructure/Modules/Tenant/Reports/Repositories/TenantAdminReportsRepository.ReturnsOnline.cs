using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Application.Modules.Tenant.Reports.Services;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;

public sealed partial class TenantAdminReportsRepository
{
    /// <summary>
    /// REP-05. One row per return line, dated by return posting (CompletedAt). Refund is a separate
    /// event: its amount only counts as refunded when <see cref="ReportRules.IsSuccessfulRefund"/>.
    /// Return-level refund figures are carried on the first line of each return so totals never double.
    /// </summary>
    private async Task<ReportResultDto> BuildReturnsResultAsync(TenantInfo tenantInfo, string section, ReportQueryRequest request,
        TenantRequestContext context, CancellationToken cancellationToken)
    {
        var outletIds = await GetAccessibleOutletIdsAsync(context, cancellationToken);
        var allowedOrders = ApplySalesFilters(await BuildOrderQueryAsync(context, request, cancellationToken),
            request with { From = null, To = null, OutletId = null, OrderStatus = null, PaymentStatus = null, Search = null });
        var returnStatus = string.IsNullOrWhiteSpace(request.ReturnStatus) ? null : request.ReturnStatus.Trim();
        var query = from r in _dbContext.SalesReturns.AsNoTracking()
                    join o in allowedOrders on r.SalesOrderId equals o.Id
                    where r.TenantId == context.TenantId && r.OutletId.HasValue && outletIds.Contains(r.OutletId.Value)
                        && (!request.OutletId.HasValue || r.OutletId == request.OutletId)
                        && (returnStatus == null || r.ReturnStatus == returnStatus)
                    select new { r, o.OrderNumber, o.SalesChannelName };
        var (fromUtc, toUtc) = ReportBusinessDateCalculator.ResolveBusinessDateRange(request.From, request.To, tenantInfo.Timezone);
        if (fromUtc.HasValue) query = query.Where(x => x.r.CompletedAt >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(x => x.r.CompletedAt < toUtc.Value);
        var returns = await query.OrderByDescending(x => x.r.CompletedAt).ThenBy(x => x.r.Id).ToListAsync(cancellationToken);
        var ids = returns.Select(x => x.r.Id).ToList();

        var lines = await (from l in _dbContext.SalesReturnLines.AsNoTracking()
                           join ol in _dbContext.SalesOrderLines.AsNoTracking() on l.SalesOrderLineId equals ol.Id
                           where l.TenantId == context.TenantId && ol.TenantId == context.TenantId && ids.Contains(l.SalesReturnId)
                           select new { l, ol.ProductId, ol.ProductVariantId, ol.ProductNameSnapshot, ol.VariantNameSnapshot, ol.SkuSnapshot, ol.UomCodeSnapshot })
            .ToListAsync(cancellationToken);
        var refunds = await _dbContext.SalesRefunds.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && x.SalesReturnId.HasValue && ids.Contains(x.SalesReturnId.Value))
            .ToListAsync(cancellationToken);
        var refundIds = refunds.Select(x => x.Id).ToList();
        var refundMethods = await (from a in _dbContext.SalesRefundPaymentAllocations.AsNoTracking()
                                   join m in _dbContext.PaymentMethods.AsNoTracking() on a.RefundPaymentMethodId equals m.Id
                                   where a.TenantId == context.TenantId && refundIds.Contains(a.SalesRefundId)
                                   select new { a.SalesRefundId, m.MethodName }).ToListAsync(cancellationToken);
        var staffIds = returns.Select(x => x.r.CreatedByTenantUserId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var staff = await _dbContext.TenantUsers.AsNoTracking().Where(x => x.TenantId == context.TenantId && staffIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName ?? x.FullName, cancellationToken);

        var search = request.Search?.Trim();
        var rows = new List<Dictionary<string, object?>>();
        foreach (var ret in returns)
        {
            var attempts = refunds.Where(x => x.SalesReturnId == ret.r.Id).OrderBy(x => x.RequestedAt).ToList();
            var successful = attempts.Where(x => ReportRules.IsSuccessfulRefund(x.RefundStatus)).Sum(x => x.RefundedAmount);
            var lineIndex = 0;
            var returnLines = lines.Where(x => x.l.SalesReturnId == ret.r.Id).OrderBy(x => x.l.Id).ToList();
            if (returnLines.Count == 0)
            {
                // A return header without stored lines stays visible rather than silently disappearing.
                if (request.ProductId.HasValue || request.ProductVariantId.HasValue) continue;
                if (!string.IsNullOrEmpty(search) && !ret.r.ReturnNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                    && !ret.OrderNumber.Contains(search, StringComparison.OrdinalIgnoreCase)) continue;
                rows.Add(Row(("returnId", ret.r.Id), ("returnNumber", ret.r.ReturnNumber), ("returnLineId", null),
                    ("originalOrderId", ret.r.SalesOrderId), ("originalOrderNumber", ret.OrderNumber),
                    ("returnPostedAt", ret.r.CompletedAt), ("completedAt", ret.r.CompletedAt),
                    ("processingOutletId", ret.r.OutletId), ("processingOutletName", ret.r.ProcessingOutletNameSnapshot),
                    ("quantity", ret.r.TotalApprovedQty ?? ret.r.TotalReceivedQty),
                    ("returnReasonCode", ret.r.ReturnReasonCodeSnapshot), ("returnReasonName", ret.r.ReturnReasonNameSnapshot),
                    ("returnStatus", ret.r.ReturnStatus),
                    ("refundStatus", attempts.Count == 0 ? "NOT_REFUNDED" : string.Join(";", attempts.Select(x => x.RefundStatus).Distinct())),
                    ("refundAmount", successful), ("refundedAmount", successful),
                    ("processedByName", ret.r.CreatedByTenantUserId.HasValue ? staff.GetValueOrDefault(ret.r.CreatedByTenantUserId.Value) : null),
                    ("currencyCode", tenantInfo.CurrencyCode)));
                continue;
            }
            foreach (var line in returnLines)
            {
                if (request.ProductId.HasValue && line.ProductId != request.ProductId) continue;
                if (request.ProductVariantId.HasValue && line.ProductVariantId != request.ProductVariantId) continue;
                if (!string.IsNullOrEmpty(search) && !ret.r.ReturnNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                    && !ret.OrderNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                    && !line.ProductNameSnapshot.Contains(search, StringComparison.OrdinalIgnoreCase)
                    && !(line.SkuSnapshot?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)) continue;
                var first = lineIndex++ == 0;
                var quantity = line.l.QuantityApproved ?? line.l.QuantityReceived ?? line.l.QuantityRequested;
                rows.Add(Row(("returnId", ret.r.Id), ("returnNumber", ret.r.ReturnNumber), ("returnLineId", line.l.Id),
                    ("originalOrderId", ret.r.SalesOrderId), ("originalOrderNumber", ret.OrderNumber),
                    ("originalSalesChannelName", ret.SalesChannelName),
                    ("returnPostedAt", ret.r.CompletedAt), ("completedAt", ret.r.CompletedAt),
                    ("processingOutletId", ret.r.OutletId), ("processingOutletName", ret.r.ProcessingOutletNameSnapshot),
                    ("productId", line.ProductId), ("productVariantId", line.ProductVariantId),
                    ("productName", line.ProductNameSnapshot), ("variantName", line.VariantNameSnapshot), ("sku", line.SkuSnapshot),
                    ("unit", line.UomCodeSnapshot), ("quantity", quantity),
                    ("requestedQuantity", line.l.QuantityRequested), ("receivedQuantity", line.l.QuantityReceived), ("approvedQuantity", line.l.QuantityApproved),
                    ("returnReasonCode", line.l.ReturnReasonCodeSnapshot ?? ret.r.ReturnReasonCodeSnapshot),
                    ("returnReasonName", line.l.ReturnReasonNameSnapshot ?? ret.r.ReturnReasonNameSnapshot),
                    ("returnValueExcludingTax", line.l.LineSubtotalAmount), ("returnTax", line.l.LineTaxAmount),
                    ("returnValueIncludingTax", line.l.LineSubtotalAmount + line.l.LineTaxAmount),
                    ("stockDisposition", line.l.DispositionStatus), ("restockable", ReportRules.IsRestockable(line.l.DispositionStatus)),
                    ("returnStatus", ret.r.ReturnStatus),
                    ("refundId", attempts.Count == 0 ? null : string.Join(";", attempts.Select(x => x.Id))),
                    ("refundNumber", attempts.Count == 0 ? null : string.Join(";", attempts.Select(x => x.RefundNumber))),
                    ("refundDate", attempts.Count == 0 ? null : (object?)attempts.Max(x => x.CompletedAt ?? x.RequestedAt)),
                    ("refundMethod", string.Join(";", refundMethods.Where(m => attempts.Any(a => a.Id == m.SalesRefundId)).Select(m => m.MethodName).Distinct())),
                    ("refundStatus", attempts.Count == 0 ? "NOT_REFUNDED" : string.Join(";", attempts.Select(x => x.RefundStatus).Distinct())),
                    ("refundOutcome", attempts.Count == 0 ? "PENDING" : attempts.Any(x => ReportRules.RefundOutcome(x.RefundStatus) == "SUCCESS") ? "SUCCESS"
                        : attempts.All(x => ReportRules.RefundOutcome(x.RefundStatus) == "FAILED") ? "FAILED" : "PENDING"),
                    ("approvedAmount", first ? attempts.Sum(x => x.ApprovedAmount) : null),
                    ("refundAmount", first ? successful : null), ("refundedAmount", first ? successful : null),
                    ("processedByUserId", ret.r.CreatedByTenantUserId),
                    ("processedByName", ret.r.CreatedByTenantUserId.HasValue ? staff.GetValueOrDefault(ret.r.CreatedByTenantUserId.Value) : null),
                    ("currencyCode", tenantInfo.CurrencyCode)));
            }
        }

        var completedIds = returns.Where(x => x.r.ReturnStatus == ReportRules.CompletedReturnStatus).Select(x => x.r.Id).ToHashSet();
        var completedLines = lines.Where(x => completedIds.Contains(x.l.SalesReturnId)).ToList();
        var pendingWithoutRefund = returns.Where(x => completedIds.Contains(x.r.Id) && refunds.All(f => f.SalesReturnId != x.r.Id))
            .Sum(x => x.r.TotalRefundAmount);
        var summary = Row(("returnCount", returns.Count), ("completedReturnCount", completedIds.Count),
            ("returnedQuantity", completedLines.Sum(x => x.l.QuantityApproved ?? 0m)),
            ("returnValueExcludingTax", completedLines.Sum(x => x.l.LineSubtotalAmount)), ("returnTax", completedLines.Sum(x => x.l.LineTaxAmount)),
            ("completedReturnValue", completedLines.Sum(x => x.l.LineSubtotalAmount + x.l.LineTaxAmount)),
            ("successfullyRefundedAmount", refunds.Where(x => ReportRules.RefundOutcome(x.RefundStatus) == "SUCCESS").Sum(x => x.RefundedAmount)),
            ("completedRefundAmount", refunds.Where(x => ReportRules.RefundOutcome(x.RefundStatus) == "SUCCESS").Sum(x => x.RefundedAmount)),
            ("pendingRefundAmount", refunds.Where(x => ReportRules.RefundOutcome(x.RefundStatus) == "PENDING")
                .Sum(x => x.ApprovedAmount > 0 ? x.ApprovedAmount : x.RequestedAmount) + pendingWithoutRefund),
            ("failedRefundAmount", refunds.Where(x => ReportRules.RefundOutcome(x.RefundStatus) == "FAILED").Sum(x => x.RequestedAmount)),
            ("restockedQuantity", completedLines.Where(x => ReportRules.IsRestockable(x.l.DispositionStatus)).Sum(x => x.l.QuantityApproved ?? 0m)),
            ("dateBasis", "RETURN POSTING DATE"));
        return Result(tenantInfo, section, request, summary,
            rows.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList(), rows.Count);
    }

    /// <summary>REP-04A: click-and-collect orders selected by placement date.</summary>
    private async Task<ReportResultDto> BuildOnlineOrdersResultAsync(TenantInfo tenantInfo, string section, ReportQueryRequest request,
        TenantRequestContext context, CancellationToken cancellationToken)
    {
        var (fromUtc, toUtc) = ReportBusinessDateCalculator.ResolveBusinessDateRange(request.From, request.To, tenantInfo.Timezone);
        var rows = await BuildOnlineOrderRowsAsync(tenantInfo, request, context,
            q => q.Where(x => (!fromUtc.HasValue || x.PlacedAt >= fromUtc) && (!toUtc.HasValue || x.PlacedAt < toUtc)), cancellationToken);
        var summary = Row(("selectedOrderCount", rows.Count), ("orderCount", rows.Count),
            ("paidAmount", rows.Sum(x => (decimal)x["paidAmount"]!)), ("outstandingAmount", rows.Sum(x => (decimal)x["outstandingAmount"]!)),
            ("totalSales", rows.Sum(x => (decimal)x["totalAmount"]!)), ("dateBasis", "ORDER PLACEMENT DATE"));
        return Result(tenantInfo, section, request, summary,
            rows.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList(), rows.Count);
    }

    /// <summary>REP-04B: uncollected, non-cancelled click-and-collect orders across all placement dates.</summary>
    private async Task<ReportResultDto> BuildCollectionsResultAsync(TenantInfo tenantInfo, string section, ReportQueryRequest request,
        TenantRequestContext context, CancellationToken cancellationToken)
    {
        var closed = ReportRules.ClosedFulfilmentStatuses;
        var rows = await BuildOnlineOrderRowsAsync(tenantInfo, request, context,
            q => q.Where(x => x.OrderStatus != "CANCELLED" && !closed.Contains(x.FulfillmentStatus)), cancellationToken);
        var summary = Row(("orderCount", rows.Count), ("totalOutstanding", rows.Sum(x => (decimal)x["outstandingAmount"]!)),
            ("paidAwaitingCollectionCount", rows.Count(x => (decimal)x["outstandingAmount"]! == 0m)),
            ("paidAmount", rows.Sum(x => (decimal)x["paidAmount"]!)),
            ("dateBasis", "ALL DATES / CURRENT WORKLOAD"));
        return Result(tenantInfo, section, request with { From = null, To = null }, summary,
            rows.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList(), rows.Count);
    }

    private async Task<List<Dictionary<string, object?>>> BuildOnlineOrderRowsAsync(TenantInfo tenantInfo, ReportQueryRequest request,
        TenantRequestContext context, Func<IQueryable<OrderProjection>, IQueryable<OrderProjection>> selection, CancellationToken ct)
    {
        const string clickAndCollect = ReportRules.ClickAndCollectOrderType;
        var orders = ApplySalesFilters(await BuildOrderQueryAsync(context, request, ct),
                request with { From = null, To = null, PaymentStatus = null })
            .Where(x => x.OrderType == clickAndCollect);
        if (!string.IsNullOrWhiteSpace(request.PaymentStatus)) orders = orders.Where(x => x.PaymentStatus == request.PaymentStatus);
        if (!string.IsNullOrWhiteSpace(request.FulfilmentStatus)) orders = orders.Where(x => x.FulfillmentStatus == request.FulfilmentStatus);
        var selected = await selection(orders).OrderByDescending(x => x.PlacedAt).ThenBy(x => x.Id).ToListAsync(ct);
        var ids = selected.Select(x => x.Id).ToList();
        var successStatuses = ReportRules.SuccessfulPaymentStatuses;
        var paid = await _dbContext.SalesPayments.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && ids.Contains(x.SalesOrderId) && successStatuses.Contains(x.PaymentStatus))
            .GroupBy(x => x.SalesOrderId).Select(g => new { g.Key, Paid = g.Sum(x => x.PaidAmount) })
            .ToDictionaryAsync(x => x.Key, x => x.Paid, ct);
        var items = await _dbContext.SalesOrderLines.AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && x.SalesOrderId.HasValue && ids.Contains(x.SalesOrderId.Value))
            .GroupBy(x => x.SalesOrderId!.Value).Select(g => new { g.Key, Items = g.Sum(x => x.Quantity), Lines = g.Count() })
            .ToDictionaryAsync(x => x.Key, ct);
        var pickups = await (from f in _dbContext.FulfillmentOrders.AsNoTracking()
                             join p in _dbContext.PickupOrders.AsNoTracking() on f.Id equals p.FulfillmentOrderId
                             where f.TenantId == context.TenantId && p.TenantId == context.TenantId && ids.Contains(f.SalesOrderId) && p.CollectedAt != null
                             select new { f.SalesOrderId, p.CollectedAt, p.VerifiedByTenantUserId }).ToListAsync(ct);
        var collectorIds = pickups.Where(x => x.VerifiedByTenantUserId.HasValue).Select(x => x.VerifiedByTenantUserId!.Value)
            .Concat(selected.Where(x => x.FulfillmentStatus == "COLLECTED" && x.UpdatedByUserId.HasValue).Select(x => x.UpdatedByUserId!.Value))
            .Distinct().ToList();
        var names = await _dbContext.TenantUsers.AsNoTracking().Where(x => x.TenantId == context.TenantId && collectorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName ?? x.FullName, ct);
        var search = request.Search?.Trim();
        return selected
            .Where(x => string.IsNullOrEmpty(search) || x.OrderNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (x.ExternalOrderReference?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(x =>
            {
                var paidAmount = paid.GetValueOrDefault(x.Id);
                var outstanding = x.OrderStatus == "CANCELLED" ? 0m : Math.Max(x.TotalAmount - paidAmount, 0m);
                var pickup = pickups.Where(p => p.SalesOrderId == x.Id).OrderByDescending(p => p.CollectedAt).FirstOrDefault();
                var collected = x.FulfillmentStatus == "COLLECTED";
                var collectedAt = pickup?.CollectedAt ?? (collected ? x.CompletedAt : null);
                var collectedBy = pickup?.VerifiedByTenantUserId ?? (collected ? x.UpdatedByUserId : null);
                items.TryGetValue(x.Id, out var count);
                return Row(("orderId", x.Id), ("orderNumber", x.OrderNumber), ("externalReference", x.ExternalOrderReference),
                    ("placedAt", x.PlacedAt), ("businessDate", x.BusinessDate),
                    ("salesChannelName", x.SalesChannelName), ("collectionOutletId", x.ReportingOutletId),
                    ("collectionOutletName", x.OutletName), ("reportingOutletName", x.OutletName),
                    ("customerReference", x.CustomerNameSnapshot), ("customerName", x.CustomerNameSnapshot),
                    ("itemCount", count?.Items ?? 0m), ("lineCount", count?.Lines ?? 0),
                    ("totalAmount", x.TotalAmount), ("paidAmount", paidAmount), ("outstandingAmount", outstanding),
                    ("paymentStatus", x.PaymentStatus), ("fulfillmentStatus", x.FulfillmentStatus), ("fulfilmentStatus", x.FulfillmentStatus),
                    ("orderStatus", x.OrderStatus), ("scheduledCollectionAt", x.RequestedCollectionAt),
                    ("collectedAt", collectedAt), ("collectedByUserId", collectedBy),
                    ("collectedByName", collectedBy.HasValue ? names.GetValueOrDefault(collectedBy.Value) : null),
                    ("cancelledAt", x.CancelledAt), ("currencyCode", x.CurrencyCode));
            }).ToList();
    }
}
