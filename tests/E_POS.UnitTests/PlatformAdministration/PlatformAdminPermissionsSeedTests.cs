using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformAdminPermissionsSeedTests
{
    [Fact]
    public void Definitions_ContainExactlyThirtySixTmEposPlatformPermissions()
    {
        Assert.Equal(36, PlatformAdminPermissionsSeedData.Definitions.Count);
        Assert.Equal(
            PlatformPermissionCodes.All.OrderBy(x => x, StringComparer.Ordinal),
            PlatformAdminPermissionsSeedData.Definitions
                .Select(x => x.PermissionCode)
                .OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void UpSql_SeedsTmEposPlatformAdminPermissionsAndSuperAdministratorRole()
    {
        var sql = PlatformAdminPermissionsSeedData.UpSql;

        Assert.Contains("permission_code", sql, StringComparison.Ordinal);
        Assert.Contains("'ACTIVE'", sql, StringComparison.Ordinal);
        Assert.Contains(PlatformPermissionCodes.DashboardView, sql, StringComparison.Ordinal);
        Assert.Contains(PlatformPermissionCodes.TenantsView, sql, StringComparison.Ordinal);
        Assert.Contains(PlatformPermissionCodes.SubscriptionPlansEdit, sql, StringComparison.Ordinal);
        Assert.Contains(PlatformPermissionCodes.PermissionsView, sql, StringComparison.Ordinal);
        Assert.Contains(PlatformPermissionCodes.RolePermissionsUpdate, sql, StringComparison.Ordinal);
        Assert.Contains(PlatformRoleCodes.SuperAdministrator, sql, StringComparison.Ordinal);
    }

    [Fact]
    public void DownSql_RemovesTmEposPlatformAdminPermissionsAndSuperAdministratorRole()
    {
        var sql = PlatformAdminPermissionsSeedData.DownSql;

        Assert.Contains(PlatformPermissionCodes.BillingManage, sql, StringComparison.Ordinal);
        Assert.Contains(PlatformRoleCodes.SuperAdministrator, sql, StringComparison.Ordinal);
    }
}

