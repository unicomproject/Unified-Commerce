using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
            Now));

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

        var repository = new TenantAdminContextRepository(dbContext);
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
            Now));

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

        var repository = new TenantAdminContextRepository(dbContext);
        var result = await repository.GetContextDataAsync(tenantUserId, tenantId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.DoesNotContain("feature_revoked", result!.EnabledFeatures);
    }

    [Fact]
    public async Task GetContextDataAsync_ReturnsActiveTenantAdminRolePermissionsAndIgnoresRevokedAssignments()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.Parse("81111111-1111-4111-8111-111111111111");
        var tenantUserId = Guid.Parse("82222222-2222-4222-8222-222222222222");
        var activeRoleId = Guid.Parse("83333333-3333-4333-8333-333333333333");
        var revokedRoleId = Guid.Parse("84444444-4444-4444-8444-444444444444");
        var moduleId = Guid.Parse("85555555-5555-4555-8555-555555555555");
        var featureId = Guid.Parse("86666666-6666-4666-8666-666666666666");
        var activePermissionId = Guid.Parse("87777777-7777-4777-8777-777777777777");
        var revokedPermissionId = Guid.Parse("88888888-8888-4888-8888-888888888888");
        var revokedDirectPermissionId = Guid.Parse("89999999-9999-4999-8999-999999999999");

        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            "TEN-ADMIN-CONTEXT",
            "ten-admin-context",
            "Tenant Admin Context",
            "active",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));

        dbContext.TenantUsers.Add(TenantUser.Create(
            tenantUserId,
            tenantId,
            "admin@test.local",
            "Tenant Admin",
            null,
            null,
            "hash",
            "salt",
            "ACTIVE",
            "admin",
            "admin",
            "HQ",
            Now));

        dbContext.PlatformModules.Add(PlatformModule.Create(
            moduleId,
            "tenant_admin_test",
            "Tenant Admin Test",
            null,
            "ACTIVE",
            1,
            Now));

        dbContext.PlatformFeatures.Add(PlatformFeature.Create(
            featureId,
            moduleId,
            "tenant_admin.dashboard",
            "Tenant Admin Dashboard",
            "ACTIVE",
            Now,
            1));

        dbContext.PermissionDefinitions.AddRange(
            PermissionDefinition.Create(
                activePermissionId,
                "tenant.dashboard.view",
                moduleId,
                featureId,
                "view",
                "View dashboard",
                true,
                true,
                Now),
            PermissionDefinition.Create(
                revokedPermissionId,
                "tenant.outlets.manage",
                moduleId,
                featureId,
                "manage",
                "Manage outlets",
                true,
                true,
                Now),
            PermissionDefinition.Create(
                revokedDirectPermissionId,
                "tenant.settings.manage",
                moduleId,
                featureId,
                "manage",
                "Manage settings",
                true,
                true,
                Now));

        dbContext.TenantRoles.AddRange(
            TenantRole.Create(
                activeRoleId,
                tenantId,
                null,
                null,
                "TENANT_ADMIN",
                "Tenant Admin",
                null,
                false,
                true,
                tenantUserId,
                Now),
            TenantRole.Create(
                revokedRoleId,
                tenantId,
                null,
                null,
                "REVOKED_ADMIN",
                "Revoked Admin",
                null,
                false,
                true,
                tenantUserId,
                Now));

        dbContext.TenantUserRoles.Add(TenantUserRole.Create(
            Guid.Parse("8aaaaaaa-0001-4000-8000-000000000001"),
            tenantId,
            tenantUserId,
            activeRoleId,
            null,
            Now));

        var revokedUserRole = TenantUserRole.Create(
            Guid.Parse("8aaaaaaa-0002-4000-8000-000000000001"),
            tenantId,
            tenantUserId,
            revokedRoleId,
            null,
            Now);
        revokedUserRole.Revoke(Now.AddMinutes(1));
        dbContext.TenantUserRoles.Add(revokedUserRole);

        dbContext.TenantRolePermissions.Add(TenantRolePermission.Create(
            Guid.Parse("8bbbbbbb-0001-4000-8000-000000000001"),
            tenantId,
            activeRoleId,
            activePermissionId,
            tenantUserId,
            Now));

        dbContext.TenantRolePermissions.Add(TenantRolePermission.Create(
            Guid.Parse("8bbbbbbb-0002-4000-8000-000000000001"),
            tenantId,
            revokedRoleId,
            revokedPermissionId,
            tenantUserId,
            Now));

        var revokedDirectPermission = TenantUserPermission.Create(
            Guid.Parse("8ccccccc-0001-4000-8000-000000000001"),
            tenantId,
            tenantUserId,
            revokedDirectPermissionId,
            tenantUserId,
            Now);
        revokedDirectPermission.Revoke(Now.AddMinutes(1));
        dbContext.TenantUserPermissions.Add(revokedDirectPermission);

        await dbContext.SaveChangesAsync();

        var repository = new TenantAdminContextRepository(dbContext);
        var result = await repository.GetContextDataAsync(tenantUserId, tenantId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains(result!.Roles, role => role.Name == "Tenant Admin");
        Assert.DoesNotContain(result.Roles, role => role.Name == "Revoked Admin");
        Assert.Contains("tenant.dashboard.view", result.EffectivePermissions);
        Assert.DoesNotContain("tenant.outlets.manage", result.EffectivePermissions);
        Assert.DoesNotContain("tenant.settings.manage", result.EffectivePermissions);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
