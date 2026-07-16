namespace E_POS.Domain.Modules.Tenant.Reports.Constants;

public static class TenantAdminReportPermissions
{
    public const string DashboardView = "tenant.reports.dashboard.view";
    public const string SalesView = "tenant.reports.sales.view";
    public const string ProductsView = "tenant.reports.products.view";
    public const string PaymentsView = "tenant.reports.payments.view";
    public const string TaxView = "tenant.reports.tax.view";
    public const string DiscountsView = "tenant.reports.discounts.view";
    public const string ReturnsView = "tenant.reports.returns.view";
    public const string CashiersView = "tenant.reports.cashiers.view";
    public const string DailySalesView = "tenant.reports.daily-sales.view";
    public const string OutletsView = "tenant.reports.outlets.view";
    public const string TillsView = "tenant.reports.tills.view";
    public const string Export = "tenant.reports.export";
    public const string CustomerPiiView = "tenant.reports.customer-pii.view";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        DashboardView,
        SalesView,
        ProductsView,
        PaymentsView,
        TaxView,
        DiscountsView,
        ReturnsView,
        CashiersView,
        DailySalesView,
        OutletsView,
        TillsView,
        Export,
        CustomerPiiView
    };
}
