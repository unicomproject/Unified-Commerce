namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentCashierTenantDashboardPermissionRemovalSeedData
{
    private static readonly IReadOnlyList<string> PermissionCodes = ["tenant.dashboard.view"];

    public static string UpSql =>
        TenantPermissionSeedSqlBuilder.BuildCashierRoleAssignmentDeleteSql(PermissionCodes);

    public static string DownSql =>
        TenantPermissionSeedSqlBuilder.BuildCashierRoleAssignmentUpsertSql(PermissionCodes);
}
