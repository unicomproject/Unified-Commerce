namespace E_POS.Infrastructure.Persistence.Seed;

public static class PlatformModuleCatalogPrerequisiteSeedConstants
{
    public static readonly Guid UserManagementModuleId =
        Guid.Parse("71500000-0000-0000-0000-000000000001");

    public static readonly Guid OutletTillCoreModuleId =
        Guid.Parse("71500000-0000-0000-0000-000000000002");

    public static readonly Guid UserAccountsFeatureId =
        Guid.Parse("72500000-0000-0000-0000-000000000001");

    public static readonly Guid OutletManagementFeatureId =
        Guid.Parse("72500000-0000-0000-0000-000000000002");

    public static readonly Guid TillManagementFeatureId =
        Guid.Parse("72500000-0000-0000-0000-000000000003");

    public const string UserManagementModuleCode = "user_management";
    public const string OutletTillCoreModuleCode = "outlet_till_core";

    public const string UserAccountsFeatureCode = "user_accounts";
    public const string OutletManagementFeatureCode = "outlet_management";
    public const string TillManagementFeatureCode = "till_management";
}
