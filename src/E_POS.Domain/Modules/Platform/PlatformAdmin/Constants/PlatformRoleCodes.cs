namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

public static class PlatformRoleCodes
{
    public const string SuperAdministrator = "super_administrator";

    /// <summary>Development-only billing viewer role (view billing, not manage).</summary>
    public const string BillingViewerDev = "billing_viewer_dev";

    /// <summary>Development-only ops role without billing permissions.</summary>
    public const string PlatformOpsNoBillingDev = "platform_ops_no_billing_dev";
}

