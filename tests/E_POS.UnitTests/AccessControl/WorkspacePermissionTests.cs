using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class WorkspacePermissionTests
{
    [Fact]
    public void CashierAllowsOnlyPosWorkspace()
    {
        Assert.Contains(WorkspacePermissions.PosAccess, TenantRoleSetupCatalog.CashierAllowedPermissionCodes);
        Assert.DoesNotContain(WorkspacePermissions.TenantAdminAccess, TenantRoleSetupCatalog.CashierAllowedPermissionCodes);
    }

    [Fact]
    public void AdminBootstrapDoesNotAcquirePosWorkspaceThroughCashierReuse()
    {
        var plan = TenantAdminBootstrapPermissionCatalog.Resolve(["pos_checkout"]);
        Assert.Contains(WorkspacePermissions.TenantAdminAccess, plan.PermissionCodes);
        Assert.DoesNotContain(WorkspacePermissions.PosAccess, plan.PermissionCodes);
    }

    [Theory]
    [InlineData(WorkspacePermissions.PosAccess)]
    [InlineData(WorkspacePermissions.TenantAdminAccess)]
    public void EffectiveResolverPreservesExplicitWorkspaceWithoutGrantingTill(string code)
    {
        var result = CashierPosEffectivePermissionResolver.Resolve([code]);
        Assert.Contains(code, result);
        Assert.DoesNotContain("pos.till.open", result);
    }

    [Fact]
    public void BackfillIsAdditiveAndPreservesRevocations()
    {
        Assert.Contains("ON CONFLICT (permission_code) DO NOTHING", WorkspacePermissionSeedData.UpSql);
        Assert.Contains("ON CONFLICT (tenant_id, role_id, permission_id) DO NOTHING", WorkspacePermissionSeedData.UpSql);
        Assert.DoesNotContain("DO UPDATE", WorkspacePermissionSeedData.UpSql);
        Assert.DoesNotContain("DELETE", WorkspacePermissionSeedData.UpSql);
    }
}
