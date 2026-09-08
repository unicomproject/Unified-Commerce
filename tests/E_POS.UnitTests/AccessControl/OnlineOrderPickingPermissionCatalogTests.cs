using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class OnlineOrderPickingPermissionCatalogTests
{
    [Fact]
    public void PickingPermissions_AreUniqueAndRegisteredForCashierSetup()
    {
        Assert.Equal(OnlineOrderPickingPermissions.All.Count,
            OnlineOrderPickingPermissions.All.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var permission in OnlineOrderPickingPermissions.All)
            Assert.Contains(permission, TenantRoleSetupCatalog.CashierAllowedPermissionCodes);
    }

    [Fact]
    public void ClickCollectEntitlement_RegistersPickingPermissionsForTenantBootstrap()
    {
        var mapped = TenantAdminBootstrapPermissionCatalog.GetMappedPermissions(
            PlatformTenantFeatureCodes.ClickCollect);

        foreach (var permission in OnlineOrderPickingPermissions.All)
            Assert.Contains(permission, mapped);
    }
}
