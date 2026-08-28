using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Infrastructure.Modules.Platform.Subscription.Services;
using E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class TenantAdminContextRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 8, 10, 20, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetContextDataAsync_IncludesEnabledFeaturesFromLegacyAndEffectiveColumns()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.Parse("61111111-1111-4111-8111-111111111111");
        var tenantUserId = Guid.Parse("62222222-2222-4222-8222-222222222222");
        var moduleId = Guid.Parse("63333333-3333-4333-8333-333333333333");
        var featureEnabledId = Guid.Parse("64444444-4444-4444-8444-444444444444");
        var featureEffectiveId = Guid.Parse("65555555-5555-4555-8555-555555555555");
        var planId = Guid.Parse("66666666-6666-4666-8666-666666666666");

        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            "TEN-CONTEXT",
            "ten-context",
            "Tenant Context",
            "active",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));

        dbContext.TenantUsers.Add(TenantUser.Create(
            tenantUserId,
            tenantId,
            "context@test.local",
            "Context User",
            null,
            null,
            "hash",
            "salt",
            "ACTIVE",
            "admin",
            "admin",
            "HQ",
            Now,
            staffCode: "USR-2026-98001"));

        dbContext.PlatformModules.Add(PlatformModule.Create(
            moduleId,
            "context_module",
            "Context Module",
            null,
            "ACTIVE",
            1,
            Now));

        dbContext.PlatformFeatures.AddRange(
            PlatformFeature.Create(
                featureEnabledId,
                moduleId,
                "feature_enabled_legacy",
                "Enabled Legacy Feature",
                "ACTIVE",
                Now,
                1),
            PlatformFeature.Create(
                featureEffectiveId,
                moduleId,
                "feature_enabled_effective",
                "Enabled Effective Feature",
                "ACTIVE",
                Now,
                2));

        dbContext.SubscriptionPlans.Add(SubscriptionPlan.Create(
            planId,
            "CONTEXT",
            "Context Plan",
            "ACTIVE",
            "MONTHLY",
            0m,
            Now));

        dbContext.TenantSubscriptions.Add(TenantSubscription.Create(
            Guid.NewGuid(),
            tenantId,
            planId,
            "ACTIVE",
            Now));

        dbContext.TenantFeatureEntitlements.AddRange(
            TenantFeatureEntitlement.Create(
                Guid.NewGuid(),
                tenantId,
                featureEnabledId,
                "ENABLED",
                Now),
            TenantFeatureEntitlement.Create(
                Guid.NewGuid(),
                tenantId,
                featureEffectiveId,
                "DISABLED",
                "MANUAL",
                sourceReferenceId: null,
                isEnabled: true,
                effectiveFrom: Now.AddMinutes(-5),
                effectiveUntil: null,
                createdByPlatformUserId: null,
                updatedByPlatformUserId: null,
                createdAt: Now));

        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var result = await repository.GetContextDataAsync(tenantUserId, tenantId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("feature_enabled_legacy", result!.EnabledFeatures);
        Assert.Contains("feature_enabled_effective", result.EnabledFeatures);
        Assert.Equal("ACTIVE", result.SubscriptionStatus);
    }

    [Fact]
    public async Task GetContextDataAsync_DoesNotReturnRevokedFeatures()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.Parse("71111111-1111-4111-8111-111111111111");
        var tenantUserId = Guid.Parse("72222222-2222-4222-8222-222222222222");
        var moduleId = Guid.Parse("73333333-3333-4333-8333-333333333333");
        var featureId = Guid.Parse("74444444-4444-4444-8444-444444444444");
        var planId = Guid.Parse("76666666-6666-4666-8666-666666666666");

        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            "TEN-CONTEXT-2",
            "ten-context-2",
            "Tenant Context 2",
            "active",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));

        dbContext.TenantUsers.Add(TenantUser.Create(
            tenantUserId,
            tenantId,
            "context2@test.local",
            "Context User 2",
            null,
            null,
            "hash",
            "salt",
            "ACTIVE",
            "admin",
            "admin",
            "HQ",
            Now,
            staffCode: "USR-2026-98002"));

        dbContext.PlatformModules.Add(PlatformModule.Create(
            moduleId,
            "context_module_2",
            "Context Module 2",
            null,
            "ACTIVE",
            1,
            Now));

        dbContext.PlatformFeatures.Add(PlatformFeature.Create(
            featureId,
            moduleId,
            "feature_revoked",
            "Revoked Feature",
            "ACTIVE",
            Now,
            1));

        dbContext.SubscriptionPlans.Add(SubscriptionPlan.Create(
            planId,
            "CONTEXT2",
            "Context Plan 2",
            "ACTIVE",
            "MONTHLY",
            0m,
            Now));

        dbContext.TenantSubscriptions.Add(TenantSubscription.Create(
            Guid.NewGuid(),
            tenantId,
            planId,
            "ACTIVE",
            Now));

        var entitlement = TenantFeatureEntitlement.Create(
            Guid.NewGuid(),
            tenantId,
            featureId,
            "ENABLED",
            Now);
        entitlement.Disable(Now.AddMinutes(1), null, "revoked", null);
        dbContext.Entry(entitlement).Property(nameof(TenantFeatureEntitlement.EntitlementStatus)).CurrentValue = "ENABLED";
        dbContext.TenantFeatureEntitlements.Add(entitlement);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var result = await repository.GetContextDataAsync(tenantUserId, tenantId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.DoesNotContain("feature_revoked", result!.EnabledFeatures);
    }

    [Fact]
    public async Task GetContextDataAsync_ExcludesRevokedPermissionsAndRevokedRolesFromAllResolutionPaths()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedPermissionFixtureAsync(dbContext, "revoked");

        var directPermission = CreatePermissionDefinition(
            fixture.ModuleId,
            fixture.FeatureId,
            "tenant.permissions.direct.revoked",
            "view",
            isActive: true);
        var tenantRolePermission = CreatePermissionDefinition(
            fixture.ModuleId,
            fixture.FeatureId,
            "tenant.permissions.role.revoked",
            "manage",
            isActive: true);
        var outletRolePermission = CreatePermissionDefinition(
            fixture.ModuleId,
            fixture.FeatureId,
            "tenant.permissions.outlet.role.revoked",
            "manage",
            isActive: true);
        var outletDirectPermission = CreatePermissionDefinition(
            fixture.ModuleId,
            fixture.FeatureId,
            "tenant.permissions.outlet.direct.revoked",
            "view",
            isActive: true);

        dbContext.PermissionDefinitions.AddRange(
            directPermission,
            tenantRolePermission,
            outletRolePermission,
            outletDirectPermission);

        var tenantRole = TenantRole.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            null,
            null,
            "REVOKED_ROLE",
            "Revoked Role",
            null,
            true,
            true,
            fixture.TenantUserId,
            Now);
        var outletRole = TenantRole.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            null,
            null,
            "REVOKED_OUTLET_ROLE",
            "Revoked Outlet Role",
            null,
            true,
            true,
            fixture.TenantUserId,
            Now);

        dbContext.TenantRoles.AddRange(tenantRole, outletRole);

        var directAssignment = TenantUserPermission.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            fixture.TenantUserId,
            directPermission.Id,
            fixture.TenantUserId,
            Now);
        directAssignment.Revoke(Now.AddMinutes(1));

        var tenantUserRole = TenantUserRole.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            fixture.TenantUserId,
            tenantRole.Id,
            fixture.TenantUserId,
            Now);
        tenantUserRole.Revoke(Now.AddMinutes(1));

        var tenantRoleGrant = TenantRolePermission.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            tenantRole.Id,
            tenantRolePermission.Id,
            fixture.TenantUserId,
            Now);

        var outlet = Outlet.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            "Revoked Outlet",
            "OUT-REVOKED",
            OutletConstants.ActiveStatus,
            OutletConstants.StoreOutletType,
            "UTC",
            true,
            null,
            null,
            null,
            Now);
        dbContext.Outlets.Add(outlet);

        var revokedOutletRole = OutletUserRole.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            outlet.Id,
            fixture.TenantUserId,
            outletRole.Id,
            fixture.TenantUserId,
            Now);
        revokedOutletRole.Revoke(fixture.TenantUserId, Now.AddMinutes(1));

        var outletRoleGrant = TenantRolePermission.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            outletRole.Id,
            outletRolePermission.Id,
            fixture.TenantUserId,
            Now);

        var revokedOutletDirect = OutletUserPermission.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            outlet.Id,
            fixture.TenantUserId,
            outletDirectPermission.Id,
            fixture.TenantUserId,
            Now);
        revokedOutletDirect.Revoke(fixture.TenantUserId, Now.AddMinutes(1));

        dbContext.TenantUserPermissions.Add(directAssignment);
        dbContext.TenantUserRoles.Add(tenantUserRole);
        dbContext.TenantRolePermissions.AddRange(tenantRoleGrant, outletRoleGrant);
        dbContext.OutletUserRoles.Add(revokedOutletRole);
        dbContext.OutletUserPermissions.Add(revokedOutletDirect);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var result = await repository.GetContextDataAsync(fixture.TenantUserId, fixture.TenantId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result!.Roles);
        Assert.DoesNotContain("tenant.permissions.direct.revoked", result.EffectivePermissions);
        Assert.DoesNotContain("tenant.permissions.role.revoked", result.EffectivePermissions);
        Assert.DoesNotContain("tenant.permissions.outlet.role.revoked", result.EffectivePermissions);
        Assert.DoesNotContain("tenant.permissions.outlet.direct.revoked", result.EffectivePermissions);
    }

    [Fact]
    public async Task GetContextDataAsync_DoesNotLeakMismatchedTenantPermissionAssignments()
    {
        await using var dbContext = CreateDbContext();
        var fixture = await SeedPermissionFixtureAsync(dbContext, "isolated");
        var foreignTenantId = Guid.NewGuid();

        dbContext.Tenants.Add(Tenant.Create(
            foreignTenantId,
            "TEN-FOREIGN",
            "ten-foreign",
            "Foreign Tenant",
            "active",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));

        var directPermission = CreatePermissionDefinition(
            fixture.ModuleId,
            fixture.FeatureId,
            "tenant.permissions.direct.foreign",
            "view",
            isActive: true);
        var tenantRolePermission = CreatePermissionDefinition(
            fixture.ModuleId,
            fixture.FeatureId,
            "tenant.permissions.role.foreign",
            "manage",
            isActive: true);
        var outletRolePermission = CreatePermissionDefinition(
            fixture.ModuleId,
            fixture.FeatureId,
            "tenant.permissions.outlet.role.foreign",
            "manage",
            isActive: true);
        var outletDirectPermission = CreatePermissionDefinition(
            fixture.ModuleId,
            fixture.FeatureId,
            "tenant.permissions.outlet.direct.foreign",
            "view",
            isActive: true);

        dbContext.PermissionDefinitions.AddRange(
            directPermission,
            tenantRolePermission,
            outletRolePermission,
            outletDirectPermission);

        var tenantRole = TenantRole.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            null,
            null,
            "SAFE_ROLE",
            "Safe Role",
            null,
            true,
            true,
            fixture.TenantUserId,
            Now);
        var outletRole = TenantRole.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            null,
            null,
            "SAFE_OUTLET_ROLE",
            "Safe Outlet Role",
            null,
            true,
            true,
            fixture.TenantUserId,
            Now);

        var outlet = Outlet.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            "Tenant Outlet",
            "OUT-SAFE",
            OutletConstants.ActiveStatus,
            OutletConstants.StoreOutletType,
            "UTC",
            true,
            null,
            null,
            null,
            Now);

        dbContext.TenantRoles.AddRange(tenantRole, outletRole);
        dbContext.Outlets.Add(outlet);
        dbContext.TenantRolePermissions.AddRange(
            TenantRolePermission.Create(Guid.NewGuid(), fixture.TenantId, tenantRole.Id, tenantRolePermission.Id, fixture.TenantUserId, Now),
            TenantRolePermission.Create(Guid.NewGuid(), fixture.TenantId, outletRole.Id, outletRolePermission.Id, fixture.TenantUserId, Now));

        dbContext.TenantUserPermissions.Add(TenantUserPermission.Create(
            Guid.NewGuid(),
            foreignTenantId,
            fixture.TenantUserId,
            directPermission.Id,
            fixture.TenantUserId,
            Now));
        dbContext.TenantUserRoles.Add(TenantUserRole.Create(
            Guid.NewGuid(),
            foreignTenantId,
            fixture.TenantUserId,
            tenantRole.Id,
            fixture.TenantUserId,
            Now));
        dbContext.OutletUserRoles.Add(OutletUserRole.Create(
            Guid.NewGuid(),
            foreignTenantId,
            outlet.Id,
            fixture.TenantUserId,
            outletRole.Id,
            fixture.TenantUserId,
            Now));
        dbContext.OutletUserPermissions.Add(OutletUserPermission.Create(
            Guid.NewGuid(),
            foreignTenantId,
            outlet.Id,
            fixture.TenantUserId,
            outletDirectPermission.Id,
            fixture.TenantUserId,
            Now));
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var result = await repository.GetContextDataAsync(fixture.TenantUserId, fixture.TenantId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result!.Roles);
        Assert.DoesNotContain("tenant.permissions.direct.foreign", result.EffectivePermissions);
        Assert.DoesNotContain("tenant.permissions.role.foreign", result.EffectivePermissions);
        Assert.DoesNotContain("tenant.permissions.outlet.role.foreign", result.EffectivePermissions);
        Assert.DoesNotContain("tenant.permissions.outlet.direct.foreign", result.EffectivePermissions);
    }

    private static TenantAdminContextRepository CreateRepository(EPosDbContext dbContext) =>
        new(
            dbContext,
            new TenantFeatureEntitlementEvaluator(
                dbContext,
                NullLogger<TenantFeatureEntitlementEvaluator>.Instance));

    private static async Task<PermissionFixture> SeedPermissionFixtureAsync(EPosDbContext dbContext, string suffix)
    {
        var tenantId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var featureId = Guid.NewGuid();

        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            $"TEN-{suffix}".ToUpperInvariant(),
            $"ten-{suffix}".ToLowerInvariant(),
            $"Tenant {suffix}",
            "active",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));

        dbContext.TenantUsers.Add(TenantUser.Create(
            tenantUserId,
            tenantId,
            $"{suffix}@test.local",
            $"User {suffix}",
            null,
            null,
            "hash",
            "salt",
            "ACTIVE",
            "admin",
            "admin",
            "HQ",
            Now,
            staffCode: $"USR-2026-{Math.Abs(tenantUserId.GetHashCode()) % 90000 + 10000:00000}"));

        dbContext.PlatformModules.Add(PlatformModule.Create(
            moduleId,
            $"module_{suffix}",
            $"Module {suffix}",
            null,
            "ACTIVE",
            1,
            Now));

        dbContext.PlatformFeatures.Add(PlatformFeature.Create(
            featureId,
            moduleId,
            $"feature_{suffix}",
            $"Feature {suffix}",
            "ACTIVE",
            Now,
            1));

        dbContext.SubscriptionPlans.Add(SubscriptionPlan.Create(
            Guid.NewGuid(),
            $"PLAN-{suffix}".ToUpperInvariant(),
            $"Plan {suffix}",
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
        return new PermissionFixture(tenantId, tenantUserId, moduleId, featureId);
    }

    private static PermissionDefinition CreatePermissionDefinition(
        Guid moduleId,
        Guid featureId,
        string permissionCode,
        string actionType,
        bool isActive)
    {
        return PermissionDefinition.Create(
            Guid.NewGuid(),
            permissionCode,
            moduleId,
            featureId,
            actionType,
            permissionCode,
            false,
            isActive,
            Now);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }

    private sealed record PermissionFixture(
        Guid TenantId,
        Guid TenantUserId,
        Guid ModuleId,
        Guid FeatureId);
}
