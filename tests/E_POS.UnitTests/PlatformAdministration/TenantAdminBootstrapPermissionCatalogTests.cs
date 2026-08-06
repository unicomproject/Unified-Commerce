using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class TenantAdminBootstrapPermissionCatalogTests
{
    [Fact]
    public void Resolve_EmptyEntitlements_ReturnsOnlyBasePermissions()
    {
        var plan = TenantAdminBootstrapPermissionCatalog.Resolve([]);

        Assert.Equal(
            TenantAdminBootstrapPermissionCatalog.BasePermissionCodes.OrderBy(x => x),
            plan.PermissionCodes.OrderBy(x => x));
        Assert.DoesNotContain("tenant.outlets.manage", plan.PermissionCodes);
        Assert.DoesNotContain("inventory.stock.view", plan.PermissionCodes);
        Assert.DoesNotContain("platform.tenants.create", plan.PermissionCodes);
    }

    [Fact]
    public void Resolve_OutletEntitlement_IncludesOutletPermissions_NotInventory()
    {
        var plan = TenantAdminBootstrapPermissionCatalog.Resolve(
        [
            PlatformTenantFeatureCodes.OutletManagement,
            PlatformTenantFeatureCodes.ProductCatalog
        ]);

        Assert.Contains("tenant.outlets.manage", plan.PermissionCodes);
        Assert.Contains("catalog.products.view", plan.PermissionCodes);
        Assert.DoesNotContain("inventory.stock.view", plan.PermissionCodes);
        Assert.DoesNotContain("pos.sale.create", plan.PermissionCodes);
    }

    [Fact]
    public void Resolve_LegacyOutletAlias_MapsToOutletPermissions()
    {
        var plan = TenantAdminBootstrapPermissionCatalog.Resolve(
        [
            PlatformTenantFeatureCodes.OutletManagementLegacyAlias
        ]);

        Assert.Contains(PlatformTenantFeatureCodes.OutletManagement, plan.EffectiveEntitlementCodes);
        Assert.Contains("tenant.outlets.manage", plan.PermissionCodes);
        Assert.DoesNotContain(PlatformTenantFeatureCodes.OutletManagementLegacyAlias, plan.EffectiveEntitlementCodes);
    }

    [Fact]
    public void Resolve_PosCheckout_DoesNotGrantCashierPermissions()
    {
        var plan = TenantAdminBootstrapPermissionCatalog.Resolve(
        [
            PlatformTenantFeatureCodes.PosCheckout
        ]);

        Assert.Contains(PlatformTenantFeatureCodes.PosCheckout, plan.IntentionallyPermissionlessEntitlements);
        Assert.DoesNotContain("pos.sale.create", plan.PermissionCodes);
        Assert.DoesNotContain("pos.till.open", plan.PermissionCodes);
    }

    [Fact]
    public void Resolve_UsersAndRoles_AreEntitlementScoped_NotBase()
    {
        var baseOnly = TenantAdminBootstrapPermissionCatalog.Resolve([]);
        Assert.DoesNotContain("tenant.users.manage", baseOnly.PermissionCodes);
        Assert.DoesNotContain("tenant.roles.manage", baseOnly.PermissionCodes);

        var withUsers = TenantAdminBootstrapPermissionCatalog.Resolve(
        [
            PlatformTenantFeatureCodes.UserAccounts,
            PlatformTenantFeatureCodes.RoleManagement
        ]);
        Assert.Contains("tenant.users.manage", withUsers.PermissionCodes);
        Assert.Contains("tenant.roles.manage", withUsers.PermissionCodes);
    }

    [Fact]
    public void Resolve_UnknownEntitlement_IsReportedAndGrantsNoArbitraryPermissions()
    {
        var plan = TenantAdminBootstrapPermissionCatalog.Resolve(["totally_unknown_module"]);

        Assert.Contains("totally_unknown_module", plan.UnknownOrUnmappedEntitlements);
        Assert.Equal(
            TenantAdminBootstrapPermissionCatalog.BasePermissionCodes.OrderBy(x => x),
            plan.PermissionCodes.OrderBy(x => x));
    }

    [Fact]
    public void IsPlatformOnlyPermission_DetectsPlatformPrefix()
    {
        Assert.True(TenantAdminBootstrapPermissionCatalog.IsPlatformOnlyPermission("platform.tenants.create"));
        Assert.False(TenantAdminBootstrapPermissionCatalog.IsPlatformOnlyPermission("tenant.users.manage"));
    }

    [Fact]
    public void Resolve_DeduplicatesPermissionsAcrossOverlappingEntitlements()
    {
        var plan = TenantAdminBootstrapPermissionCatalog.Resolve(
        [
            PlatformTenantFeatureCodes.OnlineStore,
            PlatformTenantFeatureCodes.SalesOrders,
            PlatformTenantFeatureCodes.ClickCollect
        ]);

        Assert.Equal(1, plan.PermissionCodes.Count(code => code == "fulfillment.orders.view"));
        Assert.Equal(1, plan.PermissionCodes.Count(code => code == "fulfillment.orders.manage"));
    }
}
