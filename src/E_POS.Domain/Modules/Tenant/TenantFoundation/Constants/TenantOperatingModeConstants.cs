namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

public static class TenantOperatingModeConstants
{
    public const string UnifiedEpos = "unified_epos";
    public const string PosOnlineStore = "pos_online_store";
    public const string PosOnly = "pos_only";

    public static readonly IReadOnlyList<string> All =
    [
        UnifiedEpos,
        PosOnlineStore,
        PosOnly
    ];
}

