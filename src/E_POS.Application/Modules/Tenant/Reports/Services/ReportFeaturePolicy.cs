using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Tenant.Reports.Constants;

namespace E_POS.Application.Modules.Tenant.Reports.Services;

public static class ReportFeaturePolicy
{
    public static async Task<bool> IsReportsModuleEnabledAsync(
        ITenantFeatureEntitlementEvaluator evaluator, Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var feature in TenantAdminReportFeatureCodes.ModuleAliases)
        {
            if (await evaluator.IsEnabledAsync(tenantId, feature, now, cancellationToken))
            {
                return true;
            }
        }
        return false;
    }

    public static Task<bool> IsExportEnabledAsync(
        ITenantFeatureEntitlementEvaluator evaluator, Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken) =>
        evaluator.IsEnabledAsync(tenantId, TenantAdminReportFeatureCodes.ReportExport, now, cancellationToken);

    public static async Task<bool> IsSectionEnabledAsync(
        string section,
        ITenantFeatureEntitlementEvaluator evaluator, Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!await IsReportsModuleEnabledAsync(evaluator, tenantId, now, cancellationToken))
        {
            return false;
        }

        return section switch
        {
            "transactions" or "summary" or "daily" or "payments" or "payment-transactions" or "channels" or "online" or "collections" or "products" or "categories" or "tax" or "discounts" or "returns" =>
                await evaluator.IsEnabledAsync(tenantId, TenantAdminReportFeatureCodes.SalesReports, now, cancellationToken) ||
                await evaluator.IsEnabledAsync(tenantId, TenantAdminReportFeatureCodes.ReportingAnalytics, now, cancellationToken) ||
                await evaluator.IsEnabledAsync(tenantId, TenantAdminReportFeatureCodes.ReportsAnalytics, now, cancellationToken),
            "current" or "low-stock" or "out-of-stock" or "batch-expiry" or "movements" or "valuation" =>
                await evaluator.IsEnabledAsync(tenantId, TenantAdminReportFeatureCodes.InventoryReports, now, cancellationToken) ||
                await evaluator.IsEnabledAsync(tenantId, TenantAdminReportFeatureCodes.ReportingAnalytics, now, cancellationToken) ||
                await evaluator.IsEnabledAsync(tenantId, TenantAdminReportFeatureCodes.ReportsAnalytics, now, cancellationToken),
            "cashiers" =>
                await evaluator.IsEnabledAsync(tenantId, TenantAdminReportFeatureCodes.StaffPerformanceReports, now, cancellationToken) ||
                await evaluator.IsEnabledAsync(tenantId, TenantAdminReportFeatureCodes.ReportingAnalytics, now, cancellationToken) ||
                await evaluator.IsEnabledAsync(tenantId, TenantAdminReportFeatureCodes.ReportsAnalytics, now, cancellationToken),
            _ => true
        };
    }
}
