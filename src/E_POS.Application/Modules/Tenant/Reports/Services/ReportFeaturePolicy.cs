using E_POS.Domain.Modules.Tenant.Reports.Constants;

namespace E_POS.Application.Modules.Tenant.Reports.Services;

public static class ReportFeaturePolicy
{
    public static bool IsReportsModuleEnabled(IReadOnlyCollection<string> featureCodes) =>
        featureCodes.Any(TenantAdminReportFeatureCodes.ModuleAliases.Contains);

    public static bool IsSectionEnabled(string section, IReadOnlyCollection<string> featureCodes)
    {
        if (!IsReportsModuleEnabled(featureCodes))
        {
            return false;
        }

        return section switch
        {
            "transactions" or "summary" or "daily" or "payments" or "tax" or "discounts" or "returns" =>
                featureCodes.Contains(TenantAdminReportFeatureCodes.SalesReports, StringComparer.OrdinalIgnoreCase) ||
                featureCodes.Contains(TenantAdminReportFeatureCodes.ReportingAnalytics, StringComparer.OrdinalIgnoreCase) ||
                featureCodes.Contains(TenantAdminReportFeatureCodes.ReportsAnalytics, StringComparer.OrdinalIgnoreCase),
            "current" or "low-stock" or "out-of-stock" or "batch-expiry" or "movements" or "valuation" =>
                featureCodes.Contains(TenantAdminReportFeatureCodes.InventoryReports, StringComparer.OrdinalIgnoreCase) ||
                featureCodes.Contains(TenantAdminReportFeatureCodes.ReportingAnalytics, StringComparer.OrdinalIgnoreCase) ||
                featureCodes.Contains(TenantAdminReportFeatureCodes.ReportsAnalytics, StringComparer.OrdinalIgnoreCase),
            "cashiers" =>
                featureCodes.Contains(TenantAdminReportFeatureCodes.StaffPerformanceReports, StringComparer.OrdinalIgnoreCase) ||
                featureCodes.Contains(TenantAdminReportFeatureCodes.ReportingAnalytics, StringComparer.OrdinalIgnoreCase) ||
                featureCodes.Contains(TenantAdminReportFeatureCodes.ReportsAnalytics, StringComparer.OrdinalIgnoreCase),
            _ => true
        };
    }
}
