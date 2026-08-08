namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

/// <summary>
/// Canonical Phase 4 tenant setting keys. Display labels must never be used as keys.
/// </summary>
public static class TenantSettingKeys
{
    public const string TaxPricingMode = "tax.pricing_mode";
    public const string LocaleDateFormat = "locale.date_format";
    public const string LocaleTimeFormat = "locale.time_format";
    public const string LocaleNumberFormat = "locale.number_format";
    public const string ReceiptDefaults = "receipt.defaults";
    public const string NumberingPolicies = "numbering.policies";
    public const string NotificationDefaults = "notification.defaults";
    public const string SecuritySessionPolicy = "security.session_policy";
    public const string BrandingPlaceholders = "branding.placeholders";
    public const string InventoryStockBehaviour = "inventory.stock_behaviour";
    public const string OnlineStoreDefaults = "online_store.defaults";

    public const string TaxPricingModeExclusive = "TAX_EXCLUSIVE";
    public const string TaxPricingModeInclusive = "TAX_INCLUSIVE";

    public const string SettingDefinitionStatusActive = "ACTIVE";

    public static readonly IReadOnlyList<string> CoreKeys =
    [
        TaxPricingMode,
        LocaleDateFormat,
        LocaleTimeFormat,
        LocaleNumberFormat,
        ReceiptDefaults,
        NumberingPolicies,
        NotificationDefaults,
        SecuritySessionPolicy,
        BrandingPlaceholders
    ];

    public static readonly IReadOnlyList<string> AllMvpKeys =
    [
        TaxPricingMode,
        LocaleDateFormat,
        LocaleTimeFormat,
        LocaleNumberFormat,
        ReceiptDefaults,
        NumberingPolicies,
        NotificationDefaults,
        SecuritySessionPolicy,
        BrandingPlaceholders,
        InventoryStockBehaviour,
        OnlineStoreDefaults
    ];
}
