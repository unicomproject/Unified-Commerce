using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Application.Modules.Tenant.Reports.Services;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.Reports.Repositories;

public sealed partial class TenantAdminReportsRepository
{
    /// <summary>
    /// REP-02A/REP-02B. Each persisted SalesPayment row and each refund payment allocation is one
    /// event, dated by its own event time (PaidAt/InitiatedAt, refund CompletedAt/RequestedAt).
    /// Only <see cref="ReportRules"/> successful outcomes contribute to receipt/refund totals.
    /// </summary>
    private async Task<ReportResultDto> BuildPaymentEventsAsync(TenantInfo tenant, string section,
        ReportQueryRequest request, TenantRequestContext context, CancellationToken ct)
    {
        var scopedOrders = ApplySalesFilters(await BuildOrderQueryAsync(context, request, ct),
            request with { From = null, To = null, PaymentStatus = null, OrderStatus = null });
        var (fromUtc, toUtc) = ReportBusinessDateCalculator.ResolveBusinessDateRange(request.From, request.To, tenant.Timezone);
        var payments = from p in _dbContext.SalesPayments.AsNoTracking()
                       join o in scopedOrders on p.SalesOrderId equals o.Id
                       join m in _dbContext.PaymentMethods.AsNoTracking() on p.PaymentMethodId equals m.Id
                       where p.TenantId == context.TenantId && m.TenantId == context.TenantId
                           && (!request.PaymentMethodId.HasValue || p.PaymentMethodId == request.PaymentMethodId)
                           && (!fromUtc.HasValue || (p.PaidAt ?? p.InitiatedAt) >= fromUtc)
                           && (!toUtc.HasValue || (p.PaidAt ?? p.InitiatedAt) < toUtc)
                       select new { p, o.OrderNumber, o.ReportingOutletId, o.OutletName, m.MethodName, m.MethodCode, m.MethodType };
        var paymentRows = await payments.ToListAsync(ct);
        var refunds = from a in _dbContext.SalesRefundPaymentAllocations.AsNoTracking()
                      join r in _dbContext.SalesRefunds.AsNoTracking() on a.SalesRefundId equals r.Id
                      join o in scopedOrders on r.SalesOrderId equals o.Id
                      join m in _dbContext.PaymentMethods.AsNoTracking() on a.RefundPaymentMethodId equals m.Id
                      join op in _dbContext.SalesPayments.AsNoTracking() on a.OriginalSalesPaymentId equals op.Id into originals
                      from op in originals.DefaultIfEmpty()
                      where a.TenantId == context.TenantId && r.TenantId == context.TenantId && m.TenantId == context.TenantId
                          && (!request.PaymentMethodId.HasValue || a.RefundPaymentMethodId == request.PaymentMethodId)
                          && (!fromUtc.HasValue || (r.CompletedAt ?? r.RequestedAt) >= fromUtc)
                          && (!toUtc.HasValue || (r.CompletedAt ?? r.RequestedAt) < toUtc)
                      select new { a, r, o.OrderNumber, o.ReportingOutletId, o.OutletName, m.MethodName, m.MethodCode, m.MethodType,
                          OriginalTillId = op == null ? null : op.TillId };
        var refundRows = await refunds.ToListAsync(ct);
        var events = paymentRows.Select(x => new PaymentEvent(x.p.Id, null, x.p.SalesOrderId,
            x.OrderNumber, x.ReportingOutletId, x.OutletName, x.p.TillId, x.p.PaymentMethodId, x.MethodName, x.MethodCode,
            x.MethodType, x.p.PaymentStatus, ReportRules.PaymentOutcome(x.p.PaymentStatus), x.p.PaidAt ?? x.p.InitiatedAt,
            ReportRules.IsSuccessfulPayment(x.p.PaymentStatus) ? x.p.PaidAmount : 0m, 0m,
            x.p.RequestedAmount ?? 0m, x.p.TenderedAmount ?? 0m, x.p.ChangeAmount,
            ReportRules.MaskReference(x.p.ExternalReference), x.p.PaymentNumber, "PAYMENT")).Concat(
            refundRows.Select(x => new PaymentEvent(x.a.Id, x.r.Id, x.r.SalesOrderId,
                x.OrderNumber, x.ReportingOutletId, x.OutletName, x.OriginalTillId, x.a.RefundPaymentMethodId, x.MethodName, x.MethodCode,
                x.MethodType, x.r.RefundStatus, ReportRules.RefundOutcome(x.r.RefundStatus), x.r.CompletedAt ?? x.r.RequestedAt, 0m,
                ReportRules.IsSuccessfulRefund(x.r.RefundStatus, x.a.AllocationStatus) ? x.a.AllocatedAmount : 0m,
                x.a.AllocatedAmount, 0m, 0m, ReportRules.MaskReference(x.a.ExternalReference), x.r.RefundNumber, "REFUND"))).ToList();
        var receipts = events.Sum(x => x.Receipt);
        var refunded = events.Sum(x => x.Refund);
        var net = ReportRules.CalculateNetReceipts(receipts, refunded);
        var summary = Row(("successfulReceipts", receipts), ("successfulRefunds", refunded),
            ("netReceipts", net), ("totalCollected", receipts), ("totalRefunded", refunded),
            ("netCollected", net), ("successfulPaymentCount", events.Count(x => x.EventType == "PAYMENT" && x.Outcome == "SUCCESS")),
            ("pendingPaymentCount", events.Count(x => x.EventType == "PAYMENT" && x.Outcome == "PENDING")),
            ("dateBasis", "PAYMENT / REFUND EVENT DATE"),
            ("summaryFilterBasis", "date/outlet/till/cashier/channel/method; status filters detail only"));
        List<Dictionary<string, object?>> records;
        if (section == "payments")
        {
            records = events.GroupBy(x => new { x.MethodId, x.Method, x.Code, x.Type }).Select(g => Row(
                ("paymentMethodId", g.Key.MethodId), ("paymentMethodName", g.Key.Method), ("paymentMethodCode", g.Key.Code),
                ("paymentType", g.Key.Type), ("transactionCount", g.Count(x => x.EventType == "PAYMENT")),
                ("refundCount", g.Count(x => x.EventType == "REFUND")),
                ("successfulReceipts", g.Sum(x => x.Receipt)), ("successfulRefunds", g.Sum(x => x.Refund)),
                ("netReceipts", ReportRules.CalculateNetReceipts(g.Sum(x => x.Receipt), g.Sum(x => x.Refund))),
                ("paidAmount", g.Sum(x => x.Receipt)),
                ("requestedAmount", g.Where(x => x.EventType == "PAYMENT").Sum(x => x.Requested)), ("tenderedAmount", g.Sum(x => x.Tendered)),
                ("changeAmount", g.Sum(x => x.Change)), ("refundedAmount", g.Sum(x => x.Refund)),
                ("netCollectedAmount", g.Sum(x => x.Receipt - x.Refund)),
                ("percentage", receipts == 0 ? 0m : Math.Round(g.Sum(x => x.Receipt) / receipts * 100m, 2)),
                ("currencyCode", tenant.CurrencyCode))).OrderBy(x => (string)x["paymentMethodName"]!).ToList();
        }
        else
        {
            records = events.Where(x => string.IsNullOrWhiteSpace(request.PaymentStatus) || x.Status == request.PaymentStatus)
                .OrderBy(x => x.At).ThenBy(x => x.Id).Select(x => Row(("paymentId", x.Id), ("refundId", x.RefundId),
                    ("eventType", x.EventType), ("eventReference", x.Reference),
                    ("orderId", x.OrderId), ("orderNumber", x.OrderNumber), ("outletId", x.OutletId), ("outletName", x.OutletName),
                    ("tillId", x.TillId), ("paymentMethodName", x.Method), ("paymentMethodCode", x.Code), ("provider", x.Type),
                    ("maskedReference", x.MaskedReference), ("requestedAmount", x.Requested), ("tenderedAmount", x.Tendered),
                    ("changeAmount", x.Change), ("paidAmount", x.Receipt), ("refundedAmount", x.Refund),
                    ("signedAmount", x.Receipt - x.Refund), ("paymentStatus", x.Status), ("outcome", x.Outcome),
                    ("paidAt", x.At), ("eventAt", x.At), ("currencyCode", tenant.CurrencyCode))).ToList();
        }
        var total = records.Count;
        return Result(tenant, section, request, summary,
            records.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList(), total);
    }

    private sealed record PaymentEvent(Guid Id, Guid? RefundId, Guid OrderId, string OrderNumber,
        Guid? OutletId, string? OutletName, Guid? TillId, Guid MethodId, string Method, string Code, string Type,
        string Status, string Outcome, DateTimeOffset At, decimal Receipt, decimal Refund, decimal Requested,
        decimal Tendered, decimal Change, string? MaskedReference, string Reference, string EventType);
}
