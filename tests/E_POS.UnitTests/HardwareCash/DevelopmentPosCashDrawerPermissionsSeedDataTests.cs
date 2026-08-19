using E_POS.Domain.Modules.Tenant.HardwareCash.Constants;
using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.HardwareCash;

public sealed class DevelopmentPosCashDrawerPermissionsSeedDataTests
{
    [Fact]
    public void Definitions_AreStableUniqueAndUseCashDrawerFeature()
    {
        var definitions = DevelopmentPosCashDrawerPermissionsSeedData.Definitions;

        Assert.Equal(3, definitions.Count);
        Assert.Equal(3, definitions.Select(x => x.Id).Distinct().Count());
        Assert.Equal(3, definitions.Select(x => x.PermissionCode).Distinct().Count());
        Assert.All(definitions, definition =>
        {
            Assert.Equal(DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId, definition.ModuleId);
            Assert.Equal(DevelopmentPosPermissionCatalogSeedConstants.PosCashDrawerFeatureId, definition.FeatureId);
        });
        Assert.Equal(
            new[] { CashDrawerPermissions.View, CashDrawerPermissions.Manage, CashDrawerPermissions.CreateMovement },
            definitions.Select(x => x.PermissionCode));
    }

    [Fact]
    public void CashierAssignment_GrantsViewAndMovementButNotPhysicalManage()
    {
        Assert.Contains(CashDrawerPermissions.View, DevelopmentPosCashDrawerPermissionsSeedData.CashierPermissionCodes);
        Assert.Contains(CashDrawerPermissions.CreateMovement, DevelopmentPosCashDrawerPermissionsSeedData.CashierPermissionCodes);
        Assert.DoesNotContain(CashDrawerPermissions.Manage, DevelopmentPosCashDrawerPermissionsSeedData.CashierPermissionCodes);
    }
}
