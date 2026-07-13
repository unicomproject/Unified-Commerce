namespace E_POS.Domain.Modules.Platform.Subscription.Constants;

public static class SubscriptionCatalogLimitSeedConstants
{
    public static readonly Guid MaxOutletsLimitDefinitionId =
        Guid.Parse("73000000-0000-0000-0000-000000000001");

    public static readonly Guid MaxUsersLimitDefinitionId =
        Guid.Parse("73000000-0000-0000-0000-000000000002");

    public static readonly Guid MaxTillsLimitDefinitionId =
        Guid.Parse("73000000-0000-0000-0000-000000000003");

    public const string MaxOutletsLimitKey = "max_outlets";
    public const string MaxUsersLimitKey = "max_users";
    public const string MaxTillsLimitKey = "max_tills";

    public const string OutletManagementFeatureCode = "outlet_management";
    public const string UserAccountsFeatureCode = "user_accounts";
    public const string TillManagementFeatureCode = "till_management";

    public const string ExtraOutletAddonCode = "extra_outlet";
    public const string ExtraUserAddonCode = "extra_user";
    public const string ExtraTillAddonCode = "extra_till";
}
