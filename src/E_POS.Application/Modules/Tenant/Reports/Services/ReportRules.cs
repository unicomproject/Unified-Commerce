using E_POS.Domain.Modules.Tenant.Inventory.Constants;

namespace E_POS.Application.Modules.Tenant.Reports.Services;

/// <summary>
/// Single source of Release-1 reporting status mappings and formulas.
/// Status values are the ones written by the domain (SalesPayment, SalesRefund,
/// SalesReturnLine, StockMovement); nothing here is inferred from "not FAILED".
/// </summary>
public static class ReportRules
{
    // SalesPayment: PAID on capture; PARTIALLY_REFUNDED/REFUNDED keep the original receipt
    // (refunds are separate dated events). PENDING/PAYMENT_SUBMITTED/UNPAID/FAILED/CANCELLED contribute 0.
    public static readonly string[] SuccessfulPaymentStatuses = ["PAID", "PARTIALLY_REFUNDED", "REFUNDED"];
    public static readonly string[] FailedPaymentStatuses = ["FAILED", "CANCELLED"];

    // SalesRefund / SalesRefundPaymentAllocation are written COMPLETED by the domain.
    public const string CompletedRefundStatus = "COMPLETED";
    public static readonly string[] FailedRefundStatuses = ["FAILED", "CANCELLED", "REJECTED"];

    // Sales order statuses that represent a posted (non-draft, non-cancelled) sale.
    public static readonly string[] PostedOrderStatuses = ["COMPLETED", "CONFIRMED", "ACCEPTED"];
    public const string CompletedReturnStatus = "COMPLETED";
    public const string ClickAndCollectOrderType = "CLICK_AND_COLLECT";

    // Click-and-collect fulfilment states that close the collection workload.
    public static readonly string[] ClosedFulfilmentStatuses = ["COLLECTED", "FULFILLED", "CANCELLED"];

    // SalesReturnLine.DispositionStatus written by the return flow.
    public const string RestockedDisposition = "RESTOCKED";

    public const string SaleMovementType = "SALE";
    public const string ReturnMovementType = "RETURN";

    public static bool IsSuccessfulPayment(string? status) =>
        status is not null && SuccessfulPaymentStatuses.Contains(status);

    public static string PaymentOutcome(string? status) =>
        IsSuccessfulPayment(status) ? "SUCCESS" : status is not null && FailedPaymentStatuses.Contains(status) ? "FAILED" : "PENDING";

    public static bool IsSuccessfulRefund(string? refundStatus, string? allocationStatus = CompletedRefundStatus) =>
        refundStatus == CompletedRefundStatus && allocationStatus == CompletedRefundStatus;

    public static string RefundOutcome(string? status) =>
        status == CompletedRefundStatus ? "SUCCESS" : status is not null && FailedRefundStatuses.Contains(status) ? "FAILED" : "PENDING";

    public static bool IsClosedFulfilment(string? status) =>
        status is not null && ClosedFulfilmentStatuses.Contains(status);

    public static bool IsRestockable(string? disposition) => disposition == RestockedDisposition;

    /// <summary>Classifies a signed stock-ledger row into the REP-07B reconciliation bucket.</summary>
    public static StockBucket MapStockMovement(string movementType, decimal quantityChange)
    {
        if (StockMovementConstants.StockInAliases.Contains(movementType)) return StockBucket.Receipt;
        if (movementType == SaleMovementType || StockMovementConstants.StockOutAliases.Contains(movementType)) return StockBucket.Issue;
        if (movementType == ReturnMovementType) return StockBucket.Return;
        if (StockMovementConstants.AdjustmentAliases.Contains(movementType)) return StockBucket.Adjustment;
        if (StockMovementConstants.TransferAliases.Contains(movementType))
            return quantityChange >= 0 ? StockBucket.TransferIn : StockBucket.TransferOut;
        return StockBucket.Other;
    }

    public static StockReconciliation CalculateStockReconciliation(decimal opening,
        IEnumerable<(string MovementType, decimal QuantityChange)> movements)
    {
        decimal receipts = 0, transferIn = 0, returns = 0, issues = 0, transferOut = 0, adjustments = 0, other = 0;
        foreach (var (type, change) in movements)
        {
            switch (MapStockMovement(type, change))
            {
                case StockBucket.Receipt: receipts += change; break;
                case StockBucket.TransferIn: transferIn += change; break;
                case StockBucket.Return: returns += change; break;
                case StockBucket.Issue: issues -= change; break;
                case StockBucket.TransferOut: transferOut -= change; break;
                case StockBucket.Adjustment: adjustments += change; break;
                default: other += change; break;
            }
        }
        var net = receipts + transferIn + returns - issues - transferOut + adjustments + other;
        return new(opening, receipts, transferIn, returns, issues, transferOut, adjustments, other, net, opening + net);
    }

    public static decimal CalculateExpectedCash(decimal openingFloat, decimal cashReceipts, decimal cashRefunds,
        decimal otherCashIn, decimal otherCashOut) =>
        openingFloat + cashReceipts - cashRefunds + otherCashIn - otherCashOut;

    public static decimal CalculateCashDifference(decimal countedCash, decimal expectedCash) => countedCash - expectedCash;

    public static decimal CalculateNetSales(decimal postedSalesExTax, decimal completedReturnsExTax) =>
        postedSalesExTax - completedReturnsExTax;

    public static decimal CalculateNetReceipts(decimal successfulReceipts, decimal successfulRefunds) =>
        successfulReceipts - successfulRefunds;

    /// <summary>Average uses sales incl. tax before returns over completed sales; returns never enter the denominator.</summary>
    public static decimal? CalculateAverageSale(decimal salesIncludingTax, int completedSaleCount) =>
        completedSaleCount == 0 ? null : salesIncludingTax / completedSaleCount;

    /// <summary>Keeps only the last four characters of a provider/card reference.</summary>
    public static string? MaskReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;
        var trimmed = reference.Trim();
        return trimmed.Length <= 4 ? new string('*', trimmed.Length) : new string('*', trimmed.Length - 4) + trimmed[^4..];
    }
}

/// <summary>Release-1 report identifiers keyed by the API section names.</summary>
public static class ReportCatalog
{
    private static readonly IReadOnlyDictionary<string, (string Id, string Name)> Sections =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["dashboard"] = ("REP-00", "Reports Home"),
            ["transactions"] = ("REP-01A", "Sales Transactions"),
            ["summary"] = ("REP-01A", "Sales Transactions"),
            ["channels"] = ("REP-01B", "Sales by Channel"),
            ["tax"] = ("REP-01C", "Tax Breakdown"),
            ["payments"] = ("REP-02A", "Payments by Method"),
            ["payment-transactions"] = ("REP-02B", "Payment Transactions"),
            ["tills"] = ("REP-03", "Till and Shift Closing"),
            ["online"] = ("REP-04A", "Online Orders in Selected Period"),
            ["collections"] = ("REP-04B", "Outstanding Collections"),
            ["returns"] = ("REP-05", "Returns and Refunds"),
            ["products"] = ("REP-06", "Product Sales"),
            ["current"] = ("REP-07A", "Current Stock"),
            ["movements"] = ("REP-07B", "Stock Period Movements"),
            ["daily"] = ("REP-01A-DAILY", "Daily Sales")
        };

    public static (string Id, string Name)? Describe(string? section) =>
        section is not null && Sections.TryGetValue(section, out var value) ? value : null;
}

public enum StockBucket { Receipt, TransferIn, Return, Issue, TransferOut, Adjustment, Other }

public sealed record StockReconciliation(decimal Opening, decimal Receipts, decimal TransferIn, decimal RestockableReturns,
    decimal StockIssues, decimal TransferOut, decimal SignedAdjustments, decimal OtherSignedMovements,
    decimal NetMovement, decimal Closing);
