using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.HardwareCash.Constants;
using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class TenantRoleSetupCatalogTests
{
    [Theory]
    [InlineData(ProductPosPermissions.View)]
    [InlineData(ProductPosPermissions.Search)]
    [InlineData(CashDrawerPermissions.CreateMovement)]
    public void CashierAllowedPermissionCodes_IncludeSeededNonAdministrativePosPermissions(string permissionCode)
    {
        Assert.Contains(permissionCode, TenantRoleSetupCatalog.CashierAllowedPermissionCodes);
    }

    [Fact]
    public void CashierAllowedPermissionCodes_ContainEveryDefaultCashierSeedPermission()
    {
        var defaultCashierPermissions = DevelopmentPosCashierPermissionAssignmentSeedData.PermissionCodes
            .Concat(DevelopmentPosCashDrawerPermissionsSeedData.CashierPermissionCodes)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var missingCodes = defaultCashierPermissions
            .Where(permissionCode => !TenantRoleSetupCatalog.CashierAllowedPermissionCodes.Contains(permissionCode))
            .ToArray();

        Assert.Empty(missingCodes);
    }
}
