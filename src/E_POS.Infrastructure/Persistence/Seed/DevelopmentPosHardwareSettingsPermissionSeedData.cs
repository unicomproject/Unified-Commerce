using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent development seed for the POS Hardware Testing screen permission.
/// </summary>
public static class DevelopmentPosHardwareSettingsPermissionSeedData
{
    public static readonly Guid PermissionId =
        Guid.Parse("77777777-0339-4000-8000-000000000001");

    public const string PermissionCode = PosPermissions.Hardware.Settings;

    public static TenantPermissionSeedDefinition Definition { get; } = new(
        PermissionId,
        PermissionCode,
        DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId,
        DevelopmentPosPermissionCatalogSeedConstants.PosReceiptsFeatureId,
        "hardware_settings",
        "Configure and test the receipt printer assigned to this POS device.");

    public static string UpSql =>
        TenantPermissionSeedSqlBuilder.BuildPermissionUpsertSql([Definition]);

    public static string DownSql =>
        TenantPermissionSeedSqlBuilder.BuildPermissionDeleteSql([Definition]);

    public static string CashierAssignmentUpSql =>
        TenantPermissionSeedSqlBuilder.BuildCashierRoleAssignmentUpsertSql(
            [PermissionCode]);

    public static string CashierAssignmentDownSql =>
        TenantPermissionSeedSqlBuilder.BuildCashierRoleAssignmentDeleteSql(
            [PermissionCode]);
}

