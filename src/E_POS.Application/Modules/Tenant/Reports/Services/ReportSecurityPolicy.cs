using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Domain.Modules.Tenant.Reports.Constants;

namespace E_POS.Application.Modules.Tenant.Reports.Services;

public static class ReportSecurityPolicy
{
    public static string? MaskEmail(string? email, IReadOnlyCollection<string> permissions) =>
        HasCustomerPii(permissions) ? email : null;

    public static string? MaskPhone(string? phone, IReadOnlyCollection<string> permissions) =>
        HasCustomerPii(permissions) ? phone : null;

    public static decimal? ProtectStockValue(decimal? value, IReadOnlyCollection<string> permissions) =>
        permissions.Contains(TenantAdminStockPermissions.ValueView) ? value : null;

    public static bool CanExport(IReadOnlyCollection<string> permissions, string sectionPermission) =>
        permissions.Contains(TenantAdminReportPermissions.Export) && permissions.Contains(sectionPermission);

    private static bool HasCustomerPii(IReadOnlyCollection<string> permissions) =>
        permissions.Contains(TenantAdminReportPermissions.CustomerPiiView);
}
