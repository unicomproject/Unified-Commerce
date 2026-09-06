namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

/// <summary>
/// Chunk 7: frozen sensitive field permission codes from the canonical catalog.
/// Do not invent codes here — only mirror catalog entries used for response filtering.
/// </summary>
public static class CashierPosSensitiveFieldCodes
{
    public const string HomeTotalSales = "pos.home.session_summary.total_sales";
    public const string HomeDiscounts = "pos.home.session_summary.discounts";
    public const string HomeNetSales = "pos.home.session_summary.net_sales";

    public const string HeldSalesValue = "pos.held_sales.list.value";
    public const string HeldSalesSummary = "pos.held_sales.list.summary";

    public const string CartDiscount = "pos.cart.summary.discount";
    public const string CartTotal = "pos.cart.summary.total";

    public const string CheckoutDiscount = "pos.checkout.summary.discount";
    public const string CheckoutTotal = "pos.checkout.summary.total";

    public const string CashPaymentDiscount = "pos.cash_payment.summary.discount";
    public const string CashPaymentTotalDue = "pos.cash_payment.summary.total_due";
    public const string CashPaymentAmountReceivedView = "pos.cash_payment.tender.amount_received_view";
    public const string CashPaymentDueAmount = "pos.cash_payment.tender.due_amount";
    public const string CashPaymentChangeDue = "pos.cash_payment.tender.change_due";

    public const string SaleCompleteCustomer = "pos.sale_complete.details.customer";
    public const string SaleCompleteCashReceived = "pos.sale_complete.details.cash_received";
    public const string SaleCompleteChangeDue = "pos.sale_complete.details.change_due";
    public const string SaleCompleteTotalPaid = "pos.sale_complete.details.total_paid";

    public const string ReceiptCustomer = "pos.receipts.details.customer";
    public const string ReceiptPaymentMethod = "pos.receipts.details.payment_method";
    public const string ReceiptDiscount = "pos.receipts.details.discount";
    public const string ReceiptTotal = "pos.receipts.details.total";
    public const string ReceiptPaidAmount = "pos.receipts.details.paid_amount";
    public const string ReceiptChangeDue = "pos.receipts.details.change_due";

    public const string CustomerPhone = "pos.customers.list.phone";
    public const string CustomerEmail = "pos.customers.list.email";
    public const string CustomerTotalSpend = "pos.customers.list.total_spend";
    public const string CustomerAverageOrderValue = "pos.customers.details.average_order_value";
    public const string CustomerRecentPurchases = "pos.customers.history.recent_purchases";
    public const string CustomerPurchaseAmounts = "pos.customers.history.purchase_amounts";
    public const string CustomerPurchaseHistory = "pos.customers.history.purchase_history";

    public const string DrawerOpeningCash = "pos.cash_drawer.summary.opening_cash";
    public const string DrawerCashSales = "pos.cash_drawer.summary.cash_sales";
    public const string DrawerExpectedCash = "pos.cash_drawer.summary.expected_cash";
    public const string DrawerMovementAmount = "pos.cash_drawer.movements.amount_view";

    public const string TillStartingCashView = "pos.till.opening.starting_cash_view";
    public const string TillClosingExpectedCash = "pos.till.closing.expected_cash";
    public const string TillClosingDifference = "pos.till.closing.difference";
    public const string TillClosingExpectedCashSummary = "pos.till.closing.expected_cash_summary";
    public const string TillClosingCountedCashSummary = "pos.till.closing.counted_cash_summary";
    public const string TillClosingDifferenceSummary = "pos.till.closing.difference_summary";

    public const string CashInExpectedCash = "pos.cash_movements.cash_in.expected_cash";
    public const string CashInAvailableCash = "pos.cash_movements.cash_in.available_cash";
    public const string CashInResultingBalance = "pos.cash_movements.cash_in.resulting_balance";
    public const string CashOutExpectedCash = "pos.cash_movements.cash_out.expected_cash";
    public const string CashOutAvailableCash = "pos.cash_movements.cash_out.available_cash";
    public const string CashOutResultingBalance = "pos.cash_movements.cash_out.resulting_balance";
    public const string CashDropExpectedCash = "pos.cash_movements.cash_drop.expected_cash";
    public const string CashDropAvailableCash = "pos.cash_movements.cash_drop.available_cash";
    public const string CashDropResultingBalance = "pos.cash_movements.cash_drop.resulting_balance";
}
