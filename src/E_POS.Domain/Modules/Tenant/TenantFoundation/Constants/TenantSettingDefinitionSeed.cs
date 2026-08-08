namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

/// <summary>
/// Stable seed catalog for Phase 4 MVP <c>setting_definitions</c>.
/// IDs and keys must stay aligned with the EF seed migration.
/// </summary>
public static class TenantSettingDefinitionSeed
{
    public static readonly Guid TaxPricingModeId = Guid.Parse("a1000000-0000-4000-8000-000000000001");
    public static readonly Guid LocaleDateFormatId = Guid.Parse("a1000000-0000-4000-8000-000000000002");
    public static readonly Guid LocaleTimeFormatId = Guid.Parse("a1000000-0000-4000-8000-000000000003");
    public static readonly Guid LocaleNumberFormatId = Guid.Parse("a1000000-0000-4000-8000-000000000004");
    public static readonly Guid ReceiptDefaultsId = Guid.Parse("a1000000-0000-4000-8000-000000000005");
    public static readonly Guid NumberingPoliciesId = Guid.Parse("a1000000-0000-4000-8000-000000000006");
    public static readonly Guid NotificationDefaultsId = Guid.Parse("a1000000-0000-4000-8000-000000000007");
    public static readonly Guid SecuritySessionPolicyId = Guid.Parse("a1000000-0000-4000-8000-000000000008");
    public static readonly Guid BrandingPlaceholdersId = Guid.Parse("a1000000-0000-4000-8000-000000000009");
    public static readonly Guid InventoryStockBehaviourId = Guid.Parse("a1000000-0000-4000-8000-00000000000a");
    public static readonly Guid OnlineStoreDefaultsId = Guid.Parse("a1000000-0000-4000-8000-00000000000b");

    public const string TaxPricingModeDefaultJson = "\"TAX_EXCLUSIVE\"";
    public const string LocaleDateFormatDefaultJson = "\"yyyy-MM-dd\"";
    public const string LocaleTimeFormatDefaultJson = "\"HH:mm\"";
    public const string LocaleNumberFormatDefaultJson = "\"en-LK\"";

    public const string ReceiptDefaultsDefaultJson =
        "{\"headerText\":null,\"footerText\":\"Thank you for shopping with us.\",\"showTaxBreakdown\":true}";

    public const string NumberingPoliciesDefaultJson =
        "{\"SALES_ORDER\":{\"prefix\":\"ORD-\",\"paddingLength\":6,\"resetRule\":\"NONE\"},\"POS_RECEIPT\":{\"prefix\":\"RCPT-\",\"paddingLength\":6,\"resetRule\":\"NONE\"},\"RETURN\":{\"prefix\":\"RET-\",\"paddingLength\":6,\"resetRule\":\"NONE\"}}";

    public const string NotificationDefaultsDefaultJson =
        "{\"emailEnabled\":true,\"smsEnabled\":false}";

    public const string SecuritySessionPolicyDefaultJson =
        "{\"idleTimeoutMinutes\":30}";

    public const string BrandingPlaceholdersDefaultJson =
        "{\"logoAssetId\":null,\"primaryColor\":null}";

    public const string InventoryStockBehaviourDefaultJson =
        "{\"allowNegativeStock\":false}";

    public const string OnlineStoreDefaultsDefaultJson =
        "{\"storeStatus\":\"DRAFT\",\"taxDisplayMode\":\"MATCH_TENANT\"}";

    public static IReadOnlyList<TenantSettingDefinitionSeedRow> All { get; } =
    [
        new(TaxPricingModeId, TenantSettingKeys.TaxPricingMode, "Tax pricing mode", "string",
            TaxPricingModeDefaultJson, "Default tax pricing mode for tenant sales (TAX_EXCLUSIVE or TAX_INCLUSIVE).", true, null),
        new(LocaleDateFormatId, TenantSettingKeys.LocaleDateFormat, "Date format", "string",
            LocaleDateFormatDefaultJson, "Default date display format.", true, null),
        new(LocaleTimeFormatId, TenantSettingKeys.LocaleTimeFormat, "Time format", "string",
            LocaleTimeFormatDefaultJson, "Default time display format.", true, null),
        new(LocaleNumberFormatId, TenantSettingKeys.LocaleNumberFormat, "Number format locale", "string",
            LocaleNumberFormatDefaultJson, "Locale tag used for number formatting.", true, null),
        new(ReceiptDefaultsId, TenantSettingKeys.ReceiptDefaults, "Receipt defaults", "object",
            ReceiptDefaultsDefaultJson, "MVP receipt policy defaults (not a full template graph).", true, null),
        new(NumberingPoliciesId, TenantSettingKeys.NumberingPolicies, "Numbering policies", "object",
            NumberingPoliciesDefaultJson, "MVP document numbering policies (not sequence rows).", true, null),
        new(NotificationDefaultsId, TenantSettingKeys.NotificationDefaults, "Notification defaults", "object",
            NotificationDefaultsDefaultJson, "Minimal notification preference defaults.", true, null),
        new(SecuritySessionPolicyId, TenantSettingKeys.SecuritySessionPolicy, "Session policy", "object",
            SecuritySessionPolicyDefaultJson, "Tenant-level session idle policy defaults.", false, null),
        new(BrandingPlaceholdersId, TenantSettingKeys.BrandingPlaceholders, "Branding placeholders", "object",
            BrandingPlaceholdersDefaultJson, "Minimal branding placeholder defaults.", true, null),
        new(InventoryStockBehaviourId, TenantSettingKeys.InventoryStockBehaviour, "Inventory stock behaviour", "object",
            InventoryStockBehaviourDefaultJson, "Inventory stock behaviour defaults.", true, "inventory_tracking"),
        new(OnlineStoreDefaultsId, TenantSettingKeys.OnlineStoreDefaults, "Online store defaults", "object",
            OnlineStoreDefaultsDefaultJson, "Online store operational defaults.", true, "online_store")
    ];
}

public sealed record TenantSettingDefinitionSeedRow(
    Guid Id,
    string SettingKey,
    string DisplayName,
    string ValueType,
    string DefaultValueJson,
    string Description,
    bool IsTenantEditable,
    string? RequiredFeatureCode);
