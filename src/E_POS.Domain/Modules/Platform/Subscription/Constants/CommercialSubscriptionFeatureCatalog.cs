namespace E_POS.Domain.Modules.Platform.Subscription.Constants;

/// <summary>
/// Release-1 canonical commercial subscription / tenant entitlement feature keys.
/// See Second Brain: CANONICAL_PERMISSION_AND_FEATURE_ENTITLEMENT_CONTRACT_R1.md
/// </summary>
public static class CommercialSubscriptionFeatureCatalog
{
    /// <summary>Stable id for <see cref="PlatformTenantFeatureCodes.PosCheckout"/> when seeded.</summary>
    public static readonly Guid PosCheckoutFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000023");

    private static readonly HashSet<string> CanonicalCommercialCodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            PlatformTenantFeatureCodes.TenantProfile,
            PlatformTenantFeatureCodes.TenantSettings,
            PlatformTenantFeatureCodes.RoleManagement,
            PlatformTenantFeatureCodes.PermissionManagement,
            PlatformTenantFeatureCodes.OutletManagement,
            PlatformTenantFeatureCodes.TillManagement,
            PlatformTenantFeatureCodes.ProductCatalog,
            PlatformTenantFeatureCodes.InventoryTracking,
            PlatformTenantFeatureCodes.PosCheckout,
            PlatformTenantFeatureCodes.OnlineStore,
            PlatformTenantFeatureCodes.SalesOrders,
            PlatformTenantFeatureCodes.ClickCollect,
            PlatformTenantFeatureCodes.SalesReports,
            PlatformTenantFeatureCodes.HardwareDeviceManagement,
            PlatformTenantFeatureCodes.OfflineOperationSync,
            PlatformTenantFeatureCodes.OutletManagementLegacyAlias
        };

    private static readonly HashSet<string> TechnicalPermissionGroupingCodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            PlatformTenantFeatureCodes.UserAccounts,
            "pos.cash_drawer",
            "pos.customers",
            "pos.exchanges",
            "pos.home",
            "pos.notifications",
            "pos.orders",
            "pos.payments",
            "pos.products",
            "pos.receipts",
            "pos.returns",
            "pos.sales",
            "pos.till",
            "tenant.till_ops",
            "product_barcodes",
            "product_brands",
            "product_categories",
            "product_images",
            "product_variants"
        };

    private static readonly Dictionary<string, string> TechnicalToCommercialEntitlement =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["pos.cash_drawer"] = PlatformTenantFeatureCodes.PosCheckout,
            ["pos.customers"] = PlatformTenantFeatureCodes.PosCheckout,
            ["pos.exchanges"] = PlatformTenantFeatureCodes.PosCheckout,
            ["pos.home"] = PlatformTenantFeatureCodes.PosCheckout,
            ["pos.notifications"] = PlatformTenantFeatureCodes.PosCheckout,
            ["pos.orders"] = PlatformTenantFeatureCodes.PosCheckout,
            ["pos.payments"] = PlatformTenantFeatureCodes.PosCheckout,
            ["pos.products"] = PlatformTenantFeatureCodes.PosCheckout,
            ["pos.receipts"] = PlatformTenantFeatureCodes.PosCheckout,
            ["pos.returns"] = PlatformTenantFeatureCodes.PosCheckout,
            ["pos.sales"] = PlatformTenantFeatureCodes.PosCheckout,
            ["pos.till"] = PlatformTenantFeatureCodes.PosCheckout,
            ["product_barcodes"] = PlatformTenantFeatureCodes.ProductCatalog,
            ["product_brands"] = PlatformTenantFeatureCodes.ProductCatalog,
            ["product_categories"] = PlatformTenantFeatureCodes.ProductCatalog,
            ["product_images"] = PlatformTenantFeatureCodes.ProductCatalog,
            ["product_variants"] = PlatformTenantFeatureCodes.ProductCatalog
        };

    public static IReadOnlyCollection<string> GetCanonicalCommercialFeatureCodes() =>
        CanonicalCommercialCodes.Where(code =>
                !string.Equals(code, PlatformTenantFeatureCodes.OutletManagementLegacyAlias, StringComparison.OrdinalIgnoreCase))
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static bool IsCommercialSubscriptionSelectable(string? featureCode)
    {
        if (string.IsNullOrWhiteSpace(featureCode))
        {
            return false;
        }

        var normalized = PlatformTenantFeatureCodes.NormalizeToCanonicalOrSelf(featureCode.Trim());
        return CanonicalCommercialCodes.Contains(normalized) &&
               !string.Equals(normalized, PlatformTenantFeatureCodes.OutletManagementLegacyAlias, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTechnicalPermissionGroupingFeature(string? featureCode) =>
        !string.IsNullOrWhiteSpace(featureCode) &&
        TechnicalPermissionGroupingCodes.Contains(featureCode.Trim());

    public static bool IsInvalidEntitlementToken(string? featureCode) =>
        string.IsNullOrWhiteSpace(featureCode) ||
        string.Equals(featureCode.Trim(), "tenant.till_ops", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Maps technical grouping / legacy keys to canonical commercial entitlement codes for bootstrap and persistence.
    /// Returns false for invalid tokens such as tenant.till_ops.
    /// </summary>
    public static bool TryNormalizeToCommercialEntitlement(string? featureCode, out string commercialFeatureCode)
    {
        commercialFeatureCode = string.Empty;
        if (string.IsNullOrWhiteSpace(featureCode))
        {
            return false;
        }

        var trimmed = featureCode.Trim();
        if (IsInvalidEntitlementToken(trimmed))
        {
            return false;
        }

        if (TechnicalToCommercialEntitlement.TryGetValue(trimmed, out var mapped))
        {
            commercialFeatureCode = mapped;
            return true;
        }

        if (TechnicalPermissionGroupingCodes.Contains(trimmed))
        {
            commercialFeatureCode = PlatformTenantFeatureCodes.NormalizeToCanonicalOrSelf(trimmed);
            return true;
        }

        if (PlatformTenantFeatureCodes.TryGetCanonicalFeatureCode(trimmed, out var canonical))
        {
            commercialFeatureCode = canonical;
            return IsCommercialSubscriptionSelectable(canonical);
        }

        if (IsCommercialSubscriptionSelectable(trimmed))
        {
            commercialFeatureCode = PlatformTenantFeatureCodes.NormalizeToCanonicalOrSelf(trimmed);
            return true;
        }

        return false;
    }

    public static IReadOnlyList<string> NormalizeEntitlementFeatureCodes(IEnumerable<string> featureCodes)
    {
        ArgumentNullException.ThrowIfNull(featureCodes);

        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in featureCodes)
        {
            if (TryNormalizeToCommercialEntitlement(raw, out var commercial))
            {
                normalized.Add(commercial);
            }
        }

        return normalized.OrderBy(code => code, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static IReadOnlyList<NormalizedCommercialFeature> NormalizeResolvedFeatures(
        IEnumerable<(Guid Id, string FeatureCode)> resolvedFeatures)
    {
        ArgumentNullException.ThrowIfNull(resolvedFeatures);

        var byCommercialCode = new Dictionary<string, NormalizedCommercialFeature>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, featureCode) in resolvedFeatures)
        {
            if (!TryNormalizeToCommercialEntitlement(featureCode, out var commercialCode))
            {
                continue;
            }

            if (!byCommercialCode.ContainsKey(commercialCode))
            {
                byCommercialCode[commercialCode] = new NormalizedCommercialFeature(id, commercialCode);
            }
        }

        return byCommercialCode.Values
            .OrderBy(item => item.FeatureCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

public sealed record NormalizedCommercialFeature(Guid Id, string FeatureCode);
