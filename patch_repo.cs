using System;
using System.IO;

class Program
{
    static void Main()
    {
        var path = "src/E_POS.Infrastructure/Modules/Tenant/Reports/Repositories/TenantAdminReportsRepository.cs";
        var text = File.ReadAllText(path);
        
        // Add new methods
        var method1 = @"
    private async Task<ReportResultDto> BuildChannelSalesResultAsync(TenantInfo tenantInfo, string section, ReportQueryRequest request, Guid tenantId, List<Guid> orderIds, CancellationToken cancellationToken)
    {
        var rows = await (from order in _dbContext.SalesOrders.AsNoTracking()
                          join channel in _dbContext.SalesChannels.AsNoTracking() on order.SalesChannelId equals channel.Id
                          where order.TenantId == tenantId && orderIds.Contains(order.Id)
                          group order by new { order.SalesChannelId, channel.CustomName } into g
                          select new { g.Key, Count = g.Count(), Gross = g.Sum(x => x.SubtotalAmount), Discount = g.Sum(x => x.DiscountAmount), Tax = g.Sum(x => x.TaxAmount), Refund = g.Sum(x => x.RefundedAmount), Total = g.Sum(x => x.TotalAmount) })
            .ToListAsync(cancellationToken);
        
        var total = rows.Sum(x => x.Total - x.Refund);
        var records = rows.Select(x => Row(("salesChannelName", x.Key.CustomName), ("saleCount", x.Count), ("salesExcludingTax", x.Gross - x.Discount), ("taxAmount", x.Tax), ("salesIncludingTax", x.Total), ("netAmount", x.Total - x.Refund), ("currencyCode", tenantInfo.CurrencyCode))).ToList();
        
        return Result(tenantInfo, section, request, new Dictionary<string, object?>(), records, records.Count);
    }
";
        var method2 = @"
    private async Task<ReportResultDto> BuildPaymentTransactionsResultAsync(TenantInfo tenantInfo, string section, ReportQueryRequest request, TenantRequestContext context, List<Guid> orderIds, CancellationToken cancellationToken)
    {
        var rows = await (from payment in _dbContext.SalesPayments.AsNoTracking()
                          join method in _dbContext.PaymentMethods.AsNoTracking() on payment.PaymentMethodId equals method.Id
                          join order in _dbContext.SalesOrders.AsNoTracking() on payment.SalesOrderId equals order.Id
                          where payment.TenantId == context.TenantId && orderIds.Contains(payment.SalesOrderId)
                          select new { payment.Id, order.OrderNumber, method.MethodName, payment.RequestedAmount, payment.TenderedAmount, payment.ChangeAmount, payment.PaidAmount, payment.PaymentStatus, payment.CompletedAt })
            .ToListAsync(cancellationToken);
        
        var records = rows.Select(x => Row(("paymentId", x.Id), ("orderNumber", x.OrderNumber), ("paymentMethodName", x.MethodName), ("requestedAmount", x.RequestedAmount), ("tenderedAmount", x.TenderedAmount), ("changeAmount", x.ChangeAmount), ("paidAmount", x.PaidAmount), ("paymentStatus", x.PaymentStatus), ("completedAt", x.CompletedAt), ("currencyCode", tenantInfo.CurrencyCode))).ToList();
        
        return Result(tenantInfo, section, request, new Dictionary<string, object?>(), records, records.Count);
    }
";
        text = text.Replace("private async Task<ReportResultDto> BuildCategorySalesResultAsync(", method1 + "\r\n" + method2 + "\r\n    private async Task<ReportResultDto> BuildCategorySalesResultAsync(");
        
        // Add to switch statement
        text = text.Replace("\"categories\" => await BuildCategorySalesResultAsync", "\"channels\" => await BuildChannelSalesResultAsync(tenantInfo, section, request, context.TenantId, orderIds, cancellationToken),\r\n            \"payment-transactions\" => await BuildPaymentTransactionsResultAsync(tenantInfo, section, request, context, orderIds, cancellationToken),\r\n            \"categories\" => await BuildCategorySalesResultAsync");
        
        File.WriteAllText(path, text);
    }
}
