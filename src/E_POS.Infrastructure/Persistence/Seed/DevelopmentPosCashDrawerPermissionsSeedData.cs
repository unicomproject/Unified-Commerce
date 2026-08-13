using E_POS.Domain.Modules.Tenant.HardwareCash.Constants;

namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentPosCashDrawerPermissionsSeedData
{
    public static IReadOnlyList<TenantPermissionSeedDefinition> Definitions { get; } =
    [
        new(Guid.Parse("77777777-0350-4000-8000-000000000001"), CashDrawerPermissions.View,
            DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId,
            DevelopmentPosPermissionCatalogSeedConstants.PosCashDrawerFeatureId,
            "view", "View POS cash drawer summary and movement history."),
        new(Guid.Parse("77777777-0351-4000-8000-000000000001"), CashDrawerPermissions.Manage,
            DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId,
            DevelopmentPosPermissionCatalogSeedConstants.PosCashDrawerFeatureId,
            "manage", "Manually open and manage the physical POS cash drawer."),
        new(Guid.Parse("77777777-0352-4000-8000-000000000001"), CashDrawerPermissions.CreateMovement,
            DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId,
            DevelopmentPosPermissionCatalogSeedConstants.PosCashDrawerFeatureId,
            "movement_create", "Create POS cash in, cash out, and cash drop movements."),
    ];

    public static IReadOnlyList<string> CashierPermissionCodes { get; } =
        [CashDrawerPermissions.View, CashDrawerPermissions.CreateMovement];

    public static string UpSql => TenantPermissionSeedSqlBuilder.BuildPermissionUpsertSql(Definitions);
    public static string DownSql => TenantPermissionSeedSqlBuilder.BuildPermissionDeleteSql(Definitions);
    public static string CashierAssignmentUpSql =>
        TenantPermissionSeedSqlBuilder.BuildCashierRoleAssignmentUpsertSql(CashierPermissionCodes);
    public static string CashierAssignmentDownSql =>
        TenantPermissionSeedSqlBuilder.BuildCashierRoleAssignmentDeleteSql(CashierPermissionCodes);
}
