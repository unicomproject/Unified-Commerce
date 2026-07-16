namespace E_POS.Domain.Modules.Tenant.Reports.Constants;

public static class TenantAdminReportFeatureCodes
{
    public const string ReportingAnalytics = "reporting_analytics";
    public const string ReportsAnalytics = "reports_analytics";
    public const string SalesReports = "sales_reports";
    public const string InventoryReports = "inventory_reports";
    public const string ReportExport = "report_export";
    public const string StaffPerformanceReports = "staff_performance_reports";

    public static readonly IReadOnlySet<string> ModuleAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ReportingAnalytics,
        ReportsAnalytics,
        SalesReports,
        InventoryReports,
        ReportExport,
        StaffPerformanceReports
    };
}
