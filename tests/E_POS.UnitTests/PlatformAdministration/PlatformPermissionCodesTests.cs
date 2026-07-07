using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformPermissionCodesTests
{
    [Fact]
    public void All_ContainsExactlyThirtySixTmEposPlatformPermissionCodes()
    {
        Assert.Equal(36, PlatformPermissionCodes.All.Count);
        Assert.Equal(36, PlatformPermissionCodes.All.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void All_ContainsRequiredPluralDomainPermissionCodes()
    {
        var codes = PlatformPermissionCodes.All.ToHashSet(StringComparer.Ordinal);

        Assert.Contains(PlatformPermissionCodes.DashboardView, codes);
        Assert.Contains(PlatformPermissionCodes.TenantsView, codes);
        Assert.Contains(PlatformPermissionCodes.TenantsEntitlementsUpdate, codes);
        Assert.Contains(PlatformPermissionCodes.SubscriptionPlansView, codes);
        Assert.Contains(PlatformPermissionCodes.SubscriptionPlansDelete, codes);
        Assert.Contains(PlatformPermissionCodes.ModulesView, codes);
        Assert.Contains(PlatformPermissionCodes.FeaturesView, codes);
        Assert.Contains(PlatformPermissionCodes.PermissionsView, codes);
        Assert.Contains(PlatformPermissionCodes.RolesView, codes);
        Assert.Contains(PlatformPermissionCodes.RolePermissionsUpdate, codes);
        Assert.Contains(PlatformPermissionCodes.UsersRolesAssign, codes);
        Assert.Contains(PlatformPermissionCodes.IntegrationsManage, codes);
    }
}

