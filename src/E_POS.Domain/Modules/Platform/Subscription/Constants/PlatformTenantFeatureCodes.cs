namespace E_POS.Domain.Modules.Platform.Subscription.Constants;

/// <summary>
/// Canonical commercial feature keys for tenant entitlements.
/// New writes and runtime evaluation must prefer these keys.
/// Explicit temporary aliases are listed only for approved legacy compatibility.
/// </summary>
public static class PlatformTenantFeatureCodes
{
    public const string OnlineStore = "online_store";
    public const string ClickCollect = "click_collect";
    public const string OfflineOperationSync = "offline_operation_sync";

    public const string OutletManagement = "outlet_management";
    public const string TillManagement = "till_management";
    public const string UserAccounts = "user_accounts";
    public const string ProductCatalog = "product_catalog";
    public const string InventoryTracking = "inventory_tracking";
    public const string PosCheckout = "pos_checkout";
    public const string SalesOrders = "sales_orders";
    public const string SalesReports = "sales_reports";
    public const string HardwareDeviceManagement = "hardware_device_management";
    public const string TenantSettings = "tenant_settings";
    public const string TenantProfile = "tenant_profile";
    public const string RoleManagement = "role_management";
    public const string PermissionManagement = "permission_management";

    /// <summary>
    /// Temporary Phase 1 compatibility alias for outlet management.
    /// Must not be written by new provisioning or plan seed paths.
    /// </summary>
    public const string OutletManagementLegacyAlias = "tenant_admin.outlets";

    private static readonly Dictionary<string, string> CanonicalByAlias =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [OutletManagementLegacyAlias] = OutletManagement,
            [OutletManagement] = OutletManagement,
            [TillManagement] = TillManagement,
            [UserAccounts] = UserAccounts,
            [OnlineStore] = OnlineStore,
            [ClickCollect] = ClickCollect,
            [OfflineOperationSync] = OfflineOperationSync,
            [ProductCatalog] = ProductCatalog,
            [InventoryTracking] = InventoryTracking,
            [PosCheckout] = PosCheckout,
            [SalesOrders] = SalesOrders,
            [SalesReports] = SalesReports,
            [HardwareDeviceManagement] = HardwareDeviceManagement,
            [TenantSettings] = TenantSettings,
            [TenantProfile] = TenantProfile,
            [RoleManagement] = RoleManagement,
            [PermissionManagement] = PermissionManagement
        };

    private static readonly Dictionary<string, IReadOnlyList<string>> LegacyAliasesByCanonical =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [OutletManagement] = [OutletManagementLegacyAlias]
        };

    public static bool TryGetCanonicalFeatureCode(string? featureCode, out string canonicalFeatureCode)
    {
        canonicalFeatureCode = string.Empty;
        if (string.IsNullOrWhiteSpace(featureCode))
        {
            return false;
        }

        if (CanonicalByAlias.TryGetValue(featureCode.Trim(), out var mapped))
        {
            canonicalFeatureCode = mapped;
            return true;
        }

        return false;
    }

    public static string NormalizeToCanonicalOrSelf(string featureCode)
    {
        if (TryGetCanonicalFeatureCode(featureCode, out var canonical))
        {
            return canonical;
        }

        return featureCode.Trim();
    }

    public static bool IsKnownFeatureCode(string? featureCode) =>
        !string.IsNullOrWhiteSpace(featureCode) &&
        (CanonicalByAlias.ContainsKey(featureCode.Trim()) ||
         CommercialSubscriptionFeatureCatalog.IsCommercialSubscriptionSelectable(featureCode));

    public static bool IsLegacyAlias(string? featureCode) =>
        string.Equals(featureCode?.Trim(), OutletManagementLegacyAlias, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True when the code is the canonical outlet key or its approved legacy alias.
    /// </summary>
    public static bool IsOutletManagementFeatureCode(string? featureCode) =>
        string.Equals(featureCode?.Trim(), OutletManagement, StringComparison.OrdinalIgnoreCase) ||
        IsLegacyAlias(featureCode);

    public static IReadOnlyList<string> GetLegacyAliases(string canonicalFeatureCode)
    {
        if (LegacyAliasesByCanonical.TryGetValue(canonicalFeatureCode, out var aliases))
        {
            return aliases;
        }

        return [];
    }

    /// <summary>
    /// Lookup order for Strategy B: canonical first, then approved legacy aliases only.
    /// </summary>
    public static IReadOnlyList<string> GetLookupFeatureCodes(string featureCode)
    {
        var normalized = featureCode.Trim();
        if (!TryGetCanonicalFeatureCode(normalized, out var canonical))
        {
            return [normalized];
        }

        var aliases = GetLegacyAliases(canonical);
        if (aliases.Count == 0)
        {
            return [canonical];
        }

        var codes = new List<string>(1 + aliases.Count) { canonical };
        codes.AddRange(aliases);
        return codes;
    }
}
