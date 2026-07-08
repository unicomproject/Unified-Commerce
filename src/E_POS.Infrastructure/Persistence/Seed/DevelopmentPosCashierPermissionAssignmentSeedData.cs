namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentPosCashierPermissionAssignmentSeedData
{
    public static IReadOnlyList<string> PermissionCodes { get; } =
        DevelopmentPosNewSalePermissionsSeedData.CashierPermissionCodes
            .Concat(DevelopmentPosPaymentReceiptPermissionsSeedData.CashierPermissionCodes)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static code => code, StringComparer.Ordinal)
            .ToList();

    public static string UpSql => TenantPermissionSeedSqlBuilder.BuildCashierRoleAssignmentUpsertSql(PermissionCodes);

    public static string DownSql => TenantPermissionSeedSqlBuilder.BuildCashierRoleAssignmentDeleteSql(PermissionCodes);
}
