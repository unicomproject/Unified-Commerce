using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Application.Modules.Tenant.Reports.Services;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;

public sealed partial class TenantAdminReportsRepository
{
    private static IQueryable<OrderProjection> ApplyPostedSalesFilters(IQueryable<OrderProjection> query,
        ReportQueryRequest request, string timezone)
    {
        query = ApplySalesFilters(query, request with { From = null, To = null });
        var posted = ReportRules.PostedOrderStatuses;
        var paid = ReportRules.SuccessfulPaymentStatuses;
        query = query.Where(x => posted.Contains(x.OrderStatus) && paid.Contains(x.PaymentStatus));
        var (fromUtc, toUtc) = ReportBusinessDateCalculator.ResolveBusinessDateRange(request.From, request.To, timezone);
        if (fromUtc.HasValue) query = query.Where(x => x.PostingAt >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(x => x.PostingAt < toUtc.Value);
        return query;
    }

    private async Task<IReadOnlyDictionary<string, object?>> ReconciledSalesSummaryAsync(
        TenantInfo tenant, TenantRequestContext context, ReportQueryRequest request,
        IReadOnlyList<OrderProjection> orders, CancellationToken ct)
    {
        var adjustments = await ReturnAdjustmentsAsync(tenant, context, request, ct);
        var sales = orders.Sum(x => x.TotalAmount - x.TaxAmount);
        var returns = adjustments.Sum(x => x.Base);
        var tax = orders.Sum(x => x.TaxAmount) - adjustments.Sum(x => x.Tax);
        var netSales = ReportRules.CalculateNetSales(sales, returns);
        var average = ReportRules.CalculateAverageSale(orders.Sum(x => x.TotalAmount), orders.Count);
        return Row(("salesExcludingTax", sales), ("completedReturnsExcludingTax", returns),
            ("netSalesExcludingTax", netSales), ("netTax", tax),
            ("netSalesIncludingTax", netSales + tax), ("completedSaleCount", orders.Count),
            ("averageSaleValue", average), ("postedSaleTax", orders.Sum(x => x.TaxAmount)),
            ("returnTaxAdjustment", adjustments.Sum(x => x.Tax)), ("returnAdjustmentCount", adjustments.Select(x => x.ReturnId).Distinct().Count()),
            ("dateBasis", "SALE POSTING DATE / RETURN POSTING DATE"),
            ("grossSales", sales), ("netSales", netSales + tax), ("transactionCount", orders.Count),
            ("averageOrderValue", average),
            ("totalDiscounts", orders.Sum(x => x.DiscountAmount)), ("totalTax", tax),
            ("totalReturns", returns + adjustments.Sum(x => x.Tax)));
    }

    /// <summary>
    /// Completed return lines, dated by return posting (CompletedAt), attributed to the
    /// original sale's channel and valued from the stored return-line snapshot.
    /// </summary>
    private async Task<List<ReturnAdjustment>> ReturnAdjustmentsAsync(TenantInfo tenant,
        TenantRequestContext context, ReportQueryRequest request, CancellationToken ct)
    {
        var scopedOrders = ApplySalesFilters(await BuildOrderQueryAsync(context, request, ct),
            request with { From = null, To = null, OrderStatus = null, PaymentStatus = null, OutletId = null });
        var outlets = await GetAccessibleOutletIdsAsync(context, ct);
        var query = from r in _dbContext.SalesReturns.AsNoTracking()
                    join l in _dbContext.SalesReturnLines.AsNoTracking() on r.Id equals l.SalesReturnId
                    join original in _dbContext.SalesOrderLines.AsNoTracking() on l.SalesOrderLineId equals original.Id
                    join o in scopedOrders on r.SalesOrderId equals o.Id
                    where r.TenantId == context.TenantId && l.TenantId == context.TenantId
                        && original.TenantId == context.TenantId && r.ReturnStatus == ReportRules.CompletedReturnStatus
                        && r.OutletId.HasValue && outlets.Contains(r.OutletId.Value)
                        && (!request.OutletId.HasValue || r.OutletId == request.OutletId)
                    select new { r, l, original, o.SalesChannelId, o.SalesChannelName, o.OrderNumber };
        var (fromUtc, toUtc) = ReportBusinessDateCalculator.ResolveBusinessDateRange(request.From, request.To, tenant.Timezone);
        if (fromUtc.HasValue) query = query.Where(x => x.r.CompletedAt >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(x => x.r.CompletedAt < toUtc.Value);
        var rows = await query.ToListAsync(ct);
        return rows.Select(x => new ReturnAdjustment(x.r.Id, x.r.SalesOrderId, x.original,
            x.SalesChannelId, x.SalesChannelName, x.l.QuantityApproved ?? 0m,
            x.l.LineSubtotalAmount, x.l.LineTaxAmount, x.r.ReturnNumber, x.OrderNumber, x.r.CompletedAt,
            x.r.OutletId)).ToList();
    }

    private sealed record ReturnAdjustment(Guid ReturnId, Guid OrderId, SalesOrderLine Line,
        Guid ChannelId, string ChannelName, decimal Quantity, decimal Base, decimal Tax,
        string ReturnNumber = "", string OriginalOrderNumber = "", DateTimeOffset? PostedAt = null, Guid? OutletId = null);
}
