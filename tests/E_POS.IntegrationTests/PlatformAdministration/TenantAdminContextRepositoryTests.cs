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

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
