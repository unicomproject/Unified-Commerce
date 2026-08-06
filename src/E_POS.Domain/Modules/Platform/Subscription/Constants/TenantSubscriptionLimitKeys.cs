namespace E_POS.Domain.Modules.Platform.Subscription.Constants;

/// <summary>
/// Phase 3 canonical subscription limit-key catalog for runtime enforcement.
/// Unlimited representation: <c>null</c> (never negative; never treat missing as unlimited).
/// </summary>
public static class TenantSubscriptionLimitKeys
{
    public const string MaxOutlets = SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitKey;
    public const string MaxTills = SubscriptionCatalogLimitSeedConstants.MaxTillsLimitKey;
    public const string MaxUsers = SubscriptionCatalogLimitSeedConstants.MaxUsersLimitKey;

    /// <summary>Product capacity — not seeded for Release 1; enforcement blocked.</summary>
    public const string MaxProducts = "max_products";

    /// <summary>Hardware/device capacity — not seeded for Release 1; enforcement blocked.</summary>
    public const string MaxDevices = "max_devices";

    public const string ResourceOutlets = "outlets";
    public const string ResourceTills = "tills";
    public const string ResourceUsers = "users";
    public const string ResourceProducts = "products";
    public const string ResourceDevices = "devices";

    public static readonly IReadOnlyDictionary<string, TenantSubscriptionLimitDefinition> Definitions =
        new Dictionary<string, TenantSubscriptionLimitDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [MaxOutlets] = new(
                MaxOutlets,
                ResourceOutlets,
                "outlet",
                SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId,
                PlatformTenantFeatureCodes.OutletManagement,
                RuntimeEnforcementStatus.Enforced,
                "Non-deleted outlets (ACTIVE and INACTIVE). DELETED excluded."),
            [MaxTills] = new(
                MaxTills,
                ResourceTills,
                "till",
                SubscriptionCatalogLimitSeedConstants.MaxTillsLimitDefinitionId,
                PlatformTenantFeatureCodes.TillManagement,
                RuntimeEnforcementStatus.Enforced,
                "Non-deleted tills (ACTIVE, INACTIVE, MAINTENANCE). DELETED excluded."),
            [MaxUsers] = new(
                MaxUsers,
                ResourceUsers,
                "user",
                SubscriptionCatalogLimitSeedConstants.MaxUsersLimitDefinitionId,
                PlatformTenantFeatureCodes.UserAccounts,
                RuntimeEnforcementStatus.Enforced,
                "ACTIVE and INVITED tenant users. INACTIVE (disabled) excluded."),
            [MaxProducts] = new(
                MaxProducts,
                ResourceProducts,
                "product",
                null,
                PlatformTenantFeatureCodes.ProductCatalog,
                RuntimeEnforcementStatus.BlockedPendingCanonicalDefinition,
                "Counting rule (parent vs variant vs draft) unresolved."),
            [MaxDevices] = new(
                MaxDevices,
                ResourceDevices,
                "device",
                null,
                PlatformTenantFeatureCodes.HardwareDeviceManagement,
                RuntimeEnforcementStatus.BlockedPendingCanonicalDefinition,
                "Hardware vs POS device counting unresolved; no seeded limit key.")
        };

    public static bool TryGet(string limitKey, out TenantSubscriptionLimitDefinition definition)
    {
        definition = default!;
        if (string.IsNullOrWhiteSpace(limitKey))
        {
            return false;
        }

        return Definitions.TryGetValue(limitKey.Trim(), out definition!);
    }

    public static bool IsKnown(string? limitKey) =>
        !string.IsNullOrWhiteSpace(limitKey) && Definitions.ContainsKey(limitKey.Trim());
}

public enum RuntimeEnforcementStatus
{
    Enforced = 0,
    BlockedPendingCanonicalDefinition = 1,
    Future = 2
}

public sealed record TenantSubscriptionLimitDefinition(
    string LimitKey,
    string Resource,
    string Unit,
    Guid? FeatureLimitDefinitionId,
    string? ApplicableEntitlement,
    RuntimeEnforcementStatus Status,
    string CountingRule);
