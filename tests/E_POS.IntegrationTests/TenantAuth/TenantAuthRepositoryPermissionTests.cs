using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.TenantAuth;

public sealed class TenantAuthRepositoryPermissionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetActivePermissionCodesAsync_ExcludesRevokedAssignmentsAcrossAllPaths()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedFixtureAsync(dbContext, "revoked");

        var directPermission = CreatePermissionDefinition(fixture.ModuleId, fixture.FeatureId, "tenant.auth.direct.revoked", "view");
        var rolePermission = CreatePermissionDefinition(fixture.ModuleId, fixture.FeatureId, "tenant.auth.role.revoked", "manage");
        var outletRolePermission = CreatePermissionDefinition(fixture.ModuleId, fixture.FeatureId, "tenant.auth.outlet.role.revoked", "manage");
        var outletDirectPermission = CreatePermissionDefinition(fixture.ModuleId, fixture.FeatureId, "tenant.auth.outlet.direct.revoked", "view");
        dbContext.PermissionDefinitions.AddRange(directPermission, rolePermission, outletRolePermission, outletDirectPermission);

        var tenantRole = TenantRole.Create(Guid.NewGuid(), fixture.TenantId, null, null, "AUTH_ROLE", "Auth Role", null, true, true, fixture.TenantUserId, Now);
        var outletRole = TenantRole.Create(Guid.NewGuid(), fixture.TenantId, null, null, "AUTH_OUTLET_ROLE", "Auth Outlet Role", null, true, true, fixture.TenantUserId, Now);
        var outlet = Outlet.Create(Guid.NewGuid(), fixture.TenantId, "Outlet", "OUT-AUTH", OutletConstants.ActiveStatus, OutletConstants.StoreOutletType, "UTC", true, null, null, null, Now);
        dbContext.TenantRoles.AddRange(tenantRole, outletRole);
        dbContext.Outlets.Add(outlet);

        var revokedDirect = TenantUserPermission.Create(Guid.NewGuid(), fixture.TenantId, fixture.TenantUserId, directPermission.Id, fixture.TenantUserId, Now);
        revokedDirect.Revoke(Now.AddMinutes(1));

        var revokedUserRole = TenantUserRole.Create(Guid.NewGuid(), fixture.TenantId, fixture.TenantUserId, tenantRole.Id, fixture.TenantUserId, Now);
        revokedUserRole.Revoke(Now.AddMinutes(1));

        var revokedOutletRole = OutletUserRole.Create(Guid.NewGuid(), fixture.TenantId, outlet.Id, fixture.TenantUserId, outletRole.Id, fixture.TenantUserId, Now);
        revokedOutletRole.Revoke(fixture.TenantUserId, Now.AddMinutes(1));

        var revokedOutletDirect = OutletUserPermission.Create(Guid.NewGuid(), fixture.TenantId, outlet.Id, fixture.TenantUserId, outletDirectPermission.Id, fixture.TenantUserId, Now);
        revokedOutletDirect.Revoke(fixture.TenantUserId, Now.AddMinutes(1));

        dbContext.TenantUserPermissions.Add(revokedDirect);
        dbContext.TenantUserRoles.Add(revokedUserRole);
        dbContext.OutletUserRoles.Add(revokedOutletRole);
        dbContext.OutletUserPermissions.Add(revokedOutletDirect);
        dbContext.TenantRolePermissions.AddRange(
            TenantRolePermission.Create(Guid.NewGuid(), fixture.TenantId, tenantRole.Id, rolePermission.Id, fixture.TenantUserId, Now),
            TenantRolePermission.Create(Guid.NewGuid(), fixture.TenantId, outletRole.Id, outletRolePermission.Id, fixture.TenantUserId, Now));
        await dbContext.SaveChangesAsync();

        var repository = new TenantAuthRepository(dbContext);
        var permissions = await repository.GetActivePermissionCodesAsync(fixture.TenantUserId, fixture.TenantId, CancellationToken.None);

        Assert.Empty(permissions);
    }

    [Fact]
    public async Task GetActivePermissionCodesAsync_DoesNotLeakAssignmentsFromMismatchedTenantRows()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedFixtureAsync(dbContext, "isolated");
        var foreignTenantId = Guid.NewGuid();

        dbContext.Tenants.Add(Tenant.Create(
            foreignTenantId,
            "TEN-FOREIGN-AUTH",
            "ten-foreign-auth",
            "Foreign Auth Tenant",
            TenantAuthConstants.ActiveTenantStatus,
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));

        var directPermission = CreatePermissionDefinition(fixture.ModuleId, fixture.FeatureId, "tenant.auth.direct.foreign", "view");
        var rolePermission = CreatePermissionDefinition(fixture.ModuleId, fixture.FeatureId, "tenant.auth.role.foreign", "manage");
        var outletRolePermission = CreatePermissionDefinition(fixture.ModuleId, fixture.FeatureId, "tenant.auth.outlet.role.foreign", "manage");
        var outletDirectPermission = CreatePermissionDefinition(fixture.ModuleId, fixture.FeatureId, "tenant.auth.outlet.direct.foreign", "view");
        dbContext.PermissionDefinitions.AddRange(directPermission, rolePermission, outletRolePermission, outletDirectPermission);

        var tenantRole = TenantRole.Create(Guid.NewGuid(), fixture.TenantId, null, null, "AUTH_ROLE_SAFE", "Auth Role Safe", null, true, true, fixture.TenantUserId, Now);
        var outletRole = TenantRole.Create(Guid.NewGuid(), fixture.TenantId, null, null, "AUTH_OUTLET_ROLE_SAFE", "Auth Outlet Role Safe", null, true, true, fixture.TenantUserId, Now);
        var outlet = Outlet.Create(Guid.NewGuid(), fixture.TenantId, "Outlet", "OUT-AUTH-SAFE", OutletConstants.ActiveStatus, OutletConstants.StoreOutletType, "UTC", true, null, null, null, Now);
        dbContext.TenantRoles.AddRange(tenantRole, outletRole);
        dbContext.Outlets.Add(outlet);
        dbContext.TenantRolePermissions.AddRange(
            TenantRolePermission.Create(Guid.NewGuid(), fixture.TenantId, tenantRole.Id, rolePermission.Id, fixture.TenantUserId, Now),
            TenantRolePermission.Create(Guid.NewGuid(), fixture.TenantId, outletRole.Id, outletRolePermission.Id, fixture.TenantUserId, Now));

        dbContext.TenantUserPermissions.Add(TenantUserPermission.Create(Guid.NewGuid(), foreignTenantId, fixture.TenantUserId, directPermission.Id, fixture.TenantUserId, Now));
        dbContext.TenantUserRoles.Add(TenantUserRole.Create(Guid.NewGuid(), foreignTenantId, fixture.TenantUserId, tenantRole.Id, fixture.TenantUserId, Now));
        dbContext.OutletUserRoles.Add(OutletUserRole.Create(Guid.NewGuid(), foreignTenantId, outlet.Id, fixture.TenantUserId, outletRole.Id, fixture.TenantUserId, Now));
        dbContext.OutletUserPermissions.Add(OutletUserPermission.Create(Guid.NewGuid(), foreignTenantId, outlet.Id, fixture.TenantUserId, outletDirectPermission.Id, fixture.TenantUserId, Now));
        await dbContext.SaveChangesAsync();

        var repository = new TenantAuthRepository(dbContext);
        var permissions = await repository.GetActivePermissionCodesAsync(fixture.TenantUserId, fixture.TenantId, CancellationToken.None);

        Assert.Empty(permissions);
    }

    private static async Task<AuthFixture> SeedFixtureAsync(EPosDbContext dbContext, string suffix)
    {
        var tenantId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var featureId = Guid.NewGuid();

        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            $"TEN-AUTH-{suffix}".ToUpperInvariant(),
            $"ten-auth-{suffix}".ToLowerInvariant(),
            $"Tenant Auth {suffix}",
            TenantAuthConstants.ActiveTenantStatus,
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));
        dbContext.TenantUsers.Add(TenantUser.Create(
            tenantUserId,
            tenantId,
            $"{suffix}@auth.test",
            $"Auth {suffix}",
            null,
            null,
            "hash",
            "salt",
            TenantAuthConstants.ActiveUserStatus,
            "admin",
            "admin",
            "HQ",
            Now,
            staffCode: $"USR-2026-{Math.Abs(tenantUserId.GetHashCode()) % 90000 + 10000:00000}"));
        dbContext.PlatformModules.Add(PlatformModule.Create(
            moduleId,
            $"tenant_auth_{suffix}",
            $"Tenant Auth {suffix}",
            null,
            "ACTIVE",
            1,
            Now));
        dbContext.PlatformFeatures.Add(PlatformFeature.Create(
            featureId,
            moduleId,
            $"tenant_auth_feature_{suffix}",
            $"Tenant Auth Feature {suffix}",
            "ACTIVE",
            Now,
            1));
        dbContext.SubscriptionPlans.Add(SubscriptionPlan.Create(
            Guid.NewGuid(),
            $"AUTH-{suffix}".ToUpperInvariant(),
            $"Auth Plan {suffix}",
            "ACTIVE",
            "MONTHLY",
            0m,
            Now));
        dbContext.TenantSubscriptions.Add(TenantSubscription.Create(
            Guid.NewGuid(),
            tenantId,
            dbContext.SubscriptionPlans.Local.Last().Id,
            "ACTIVE",
            Now));
        await dbContext.SaveChangesAsync();
        return new AuthFixture(tenantId, tenantUserId, moduleId, featureId);
    }

    private static PermissionDefinition CreatePermissionDefinition(Guid moduleId, Guid featureId, string permissionCode, string actionType) =>
        PermissionDefinition.Create(
            Guid.NewGuid(),
            permissionCode,
            moduleId,
            featureId,
            actionType,
            permissionCode,
            false,
            true,
            Now);

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }

    private sealed record AuthFixture(Guid TenantId, Guid TenantUserId, Guid ModuleId, Guid FeatureId);
}
