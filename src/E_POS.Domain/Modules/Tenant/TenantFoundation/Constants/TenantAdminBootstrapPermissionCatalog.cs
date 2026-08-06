using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

/// <summary>
/// Phase 2 canonical source of truth for Bootstrap Tenant Admin permission grants.
/// Permissions are derived from effective entitlements; Platform permissions are never granted.
/// </summary>
public static class TenantAdminBootstrapPermissionCatalog
{
    /// <summary>
    /// Entitlement-independent Tenant Admin account/setup permissions.
    /// Users/roles/module ops are NOT base — they require entitlements.
    /// </summary>
    public static readonly IReadOnlyList<string> BasePermissionCodes =
    [
        "tenant.dashboard.view", // Basic home / account landing
        "tenant.settings.manage" // Basic tenant settings required for initial setup
    ];

    /// <summary>
    /// Legacy fixed bootstrap list retained only for discovery/compatibility documentation.
    /// New provisioning must use <see cref="Resolve"/>.
    /// </summary>
    [Obsolete("Use TenantAdminBootstrapPermissionCatalog.Resolve with effective entitlements.")]
    public static IReadOnlyList<string> LegacyFixedBootstrapPermissionCodes =>
        TenantCreateWizardReferenceData.TenantAdminBootstrapPermissionCodes;

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> PermissionsByEntitlement =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [PlatformTenantFeatureCodes.OutletManagement] =
            [
                TenantAdminOutletPermissions.View,
                TenantAdminOutletPermissions.DetailsView,
                TenantAdminOutletPermissions.Update,
                TenantAdminOutletPermissions.Manage
            ],
            [PlatformTenantFeatureCodes.TillManagement] =
            [
                TenantAdminTillPermissions.View,
                TenantAdminTillPermissions.Create,
                TenantAdminTillPermissions.Update,
                TenantAdminTillPermissions.Delete,
                TenantAdminTillPermissions.Manage,
                TenantAdminTillPermissions.AssignOutlet,
                TenantAdminTillPermissions.DetailsView
            ],
            [PlatformTenantFeatureCodes.ProductCatalog] =
            [
                "catalog.products.view",
                "catalog.products.create",
                "catalog.products.update"
            ],
            [PlatformTenantFeatureCodes.InventoryTracking] =
            [
                "inventory.stock.view"
            ],
            [PlatformTenantFeatureCodes.SalesReports] =
            [
                "reports.sales.view"
            ],
            [PlatformTenantFeatureCodes.OnlineStore] =
            [
                "fulfillment.orders.view",
                "fulfillment.orders.manage"
            ],
            [PlatformTenantFeatureCodes.SalesOrders] =
            [
                "fulfillment.orders.view",
                "fulfillment.orders.manage"
            ],
            [PlatformTenantFeatureCodes.ClickCollect] =
            [
                "fulfillment.orders.view",
                "fulfillment.orders.manage"
            ],
            [PlatformTenantFeatureCodes.UserAccounts] =
            [
                "tenant.users.manage"
            ],
            [PlatformTenantFeatureCodes.RoleManagement] =
            [
                "tenant.roles.manage"
            ],
            [PlatformTenantFeatureCodes.PermissionManagement] =
            [
                "tenant.roles.manage"
            ],
            [PlatformTenantFeatureCodes.TenantSettings] =
            [
                "tenant.settings.manage"
            ],
            [PlatformTenantFeatureCodes.TenantProfile] =
            [
                "tenant.dashboard.view"
            ],
            [PlatformTenantFeatureCodes.HardwareDeviceManagement] =
            [
                "tenant.devices.view",
                "tenant.devices.manage"
            ],
            // POS checkout is a commercial entitlement. Bootstrap Tenant Admin is not a cashier —
            // cashier POS operational permissions are intentionally not auto-granted.
            [PlatformTenantFeatureCodes.PosCheckout] = [],
            [PlatformTenantFeatureCodes.OfflineOperationSync] = []
        };

    public static bool IsPlatformOnlyPermission(string? permissionCode) =>
        !string.IsNullOrWhiteSpace(permissionCode) &&
        permissionCode.Trim().StartsWith("platform.", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<string> GetMappedPermissions(string entitlementFeatureCode)
    {
        var canonical = PlatformTenantFeatureCodes.NormalizeToCanonicalOrSelf(entitlementFeatureCode);
        if (PermissionsByEntitlement.TryGetValue(canonical, out var permissions))
        {
            return permissions;
        }

        return [];
    }

    public static bool HasExplicitMapping(string entitlementFeatureCode)
    {
        var canonical = PlatformTenantFeatureCodes.NormalizeToCanonicalOrSelf(entitlementFeatureCode);
        return PermissionsByEntitlement.ContainsKey(canonical);
    }

    /// <summary>
    /// Builds the final Bootstrap Tenant Admin permission set from effective feature codes.
    /// </summary>
    public static TenantAdminBootstrapPermissionPlan Resolve(IEnumerable<string> effectiveFeatureCodes)
    {
        ArgumentNullException.ThrowIfNull(effectiveFeatureCodes);

        var effectiveCanonical = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unknownEntitlements = new List<string>();
        var intentionallyPermissionless = new List<string>();

        foreach (var raw in effectiveFeatureCodes)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var trimmed = raw.Trim();
            var canonical = PlatformTenantFeatureCodes.NormalizeToCanonicalOrSelf(trimmed);
            effectiveCanonical.Add(canonical);

            if (!HasExplicitMapping(canonical))
            {
                // Unknown/unmapped commercial keys must not grant arbitrary permissions.
                if (!PlatformTenantFeatureCodes.IsKnownFeatureCode(canonical) ||
                    !PermissionsByEntitlement.ContainsKey(canonical))
                {
                    unknownEntitlements.Add(trimmed);
                }
            }
            else if (GetMappedPermissions(canonical).Count == 0)
            {
                intentionallyPermissionless.Add(canonical);
            }
        }

        var granted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var baseCode in BasePermissionCodes)
        {
            if (!IsPlatformOnlyPermission(baseCode))
            {
                granted.Add(baseCode);
            }
        }

        foreach (var entitlement in effectiveCanonical)
        {
            foreach (var permission in GetMappedPermissions(entitlement))
            {
                if (IsPlatformOnlyPermission(permission))
                {
                    continue;
                }

                granted.Add(permission);
            }
        }

        // Defence in depth: strip any platform.* that somehow entered the set.
        granted.RemoveWhere(IsPlatformOnlyPermission);

        return new TenantAdminBootstrapPermissionPlan(
            PermissionCodes: granted.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            EffectiveEntitlementCodes: effectiveCanonical.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            UnknownOrUnmappedEntitlements: unknownEntitlements
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            IntentionallyPermissionlessEntitlements: intentionallyPermissionless
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList());
    }
}

public sealed record TenantAdminBootstrapPermissionPlan(
    IReadOnlyList<string> PermissionCodes,
    IReadOnlyList<string> EffectiveEntitlementCodes,
    IReadOnlyList<string> UnknownOrUnmappedEntitlements,
    IReadOnlyList<string> IntentionallyPermissionlessEntitlements);
