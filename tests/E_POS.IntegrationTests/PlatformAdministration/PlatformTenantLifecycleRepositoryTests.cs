using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformTenantLifecycleRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 19, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateActivateSuspendFlow_PersistsExpectedStatuses()
    {
        await using var dbContext = CreateDbContext();
        var planId = Guid.Parse("77777777-7777-4777-8777-777777777799");
        var featureId = Guid.Parse("88888888-8888-4888-8888-888888888899");
        await SeedPlanWithFeatureAsync(dbContext, planId, featureId);

        IPlatformTenantRepository repository = new PlatformTenantRepository(dbContext);
        var tenantId = Guid.NewGuid();
        var tenant = Tenant.Create(tenantId, "TEN-LIFECYCLE", "ten-lifecycle", "Lifecycle Tenant", TenantStatusConstants.Draft, "LKR", "Asia/Colombo", null, null, Now);

        var subscription = TenantSubscription.Create(
            Guid.NewGuid(),
            tenantId,
            planId,
            TenantSubscriptionStatusConstants.Trial,
            Now);

        await repository.AddTenantWithSubscriptionAndEntitlementsAsync(
            tenant,
            subscription,
            [featureId],
            Now,
            CancellationToken.None);

        var draftDetail = await repository.GetTenantDetailAsync(tenantId, CancellationToken.None);
        Assert.NotNull(draftDetail);
        Assert.Equal(TenantStatusConstants.Draft, draftDetail!.Status);
        Assert.True(draftDetail.OnlineStoreEnabled);

        var loadedTenant = await repository.GetTenantEntityByIdAsync(tenantId, CancellationToken.None);
        Assert.NotNull(loadedTenant);
        loadedTenant!.Activate(null, Now);
        await repository.UpdateTenantAsync(loadedTenant, CancellationToken.None);

        var loadedSubscription = await repository.GetCurrentTenantSubscriptionEntityAsync(
            tenantId,
            CancellationToken.None);
        Assert.NotNull(loadedSubscription);
        loadedSubscription!.Activate(Now);
        await repository.UpdateTenantSubscriptionAsync(loadedSubscription, CancellationToken.None);

        var activeDetail = await repository.GetTenantDetailAsync(tenantId, CancellationToken.None);
        Assert.Equal(TenantStatusConstants.Active, activeDetail!.Status);
        Assert.Equal(TenantSubscriptionStatusConstants.Active, activeDetail.Subscription!.SubscriptionStatus);

        loadedTenant.Suspend(null, Now);
        await repository.UpdateTenantAsync(loadedTenant, CancellationToken.None);

        var suspendedDetail = await repository.GetTenantDetailAsync(tenantId, CancellationToken.None);
        Assert.Equal(TenantStatusConstants.Suspended, suspendedDetail!.Status);
    }

    [Fact]
    public async Task ReplaceTenantEntitlementsAsync_UsesSoftRevokeAndReEnableWithoutDuplicates()
    {
        await using var dbContext = CreateDbContext();
        var planId = Guid.Parse("77777777-7777-4777-8777-777777777798");
        var onlineStoreFeatureId = Guid.Parse("88888888-8888-4888-8888-888888888801");
        var clickCollectFeatureId = Guid.Parse("88888888-8888-4888-8888-888888888802");
        var actorId = Guid.Parse("88888888-8888-4888-8888-888888888899");
        await SeedPlanWithFeaturesAsync(
            dbContext,
            planId,
            [onlineStoreFeatureId, clickCollectFeatureId]);

        var tenantId = Guid.NewGuid();
        dbContext.Tenants.Add(Tenant.Create(tenantId, "TEN-ENT", "ten-ent", "Entitlement Tenant", TenantStatusConstants.Active, "LKR", "Asia/Colombo", null, null, Now));
        dbContext.TenantSubscriptions.Add(TenantSubscription.Create(
            Guid.NewGuid(),
            tenantId,
            planId,
            TenantSubscriptionStatusConstants.Active,
            Now));
        dbContext.TenantFeatureEntitlements.Add(TenantFeatureEntitlement.Create(
            Guid.Parse("89999999-9999-4999-8999-999999999901"),
            tenantId,
            onlineStoreFeatureId,
            TenantEntitlementStatusConstants.Enabled,
            Now));
        await dbContext.SaveChangesAsync();

        IPlatformTenantRepository repository = new PlatformTenantRepository(dbContext);
        await repository.ReplaceTenantEntitlementsAsync(
            tenantId,
            [clickCollectFeatureId],
            Now,
            actorId,
            "Removed during test",
            CancellationToken.None);

        var firstPassEntitlements = await dbContext.TenantFeatureEntitlements
            .Where(item => item.TenantId == tenantId)
            .OrderBy(item => item.PlatformFeatureId)
            .ToListAsync();

        Assert.Equal(2, firstPassEntitlements.Count);

        var revokedOnlineStore = Assert.Single(firstPassEntitlements, x => x.PlatformFeatureId == onlineStoreFeatureId);
        Assert.Equal(TenantEntitlementStatusConstants.Disabled, revokedOnlineStore.EntitlementStatus);
        Assert.False(revokedOnlineStore.IsEnabled);
        Assert.Equal(Now, revokedOnlineStore.RevokedAt);
        Assert.Equal(actorId, revokedOnlineStore.RevokedByPlatformUserId);
        Assert.Equal("Removed during test", revokedOnlineStore.RevokedReason);

        var newClickCollect = Assert.Single(firstPassEntitlements, x => x.PlatformFeatureId == clickCollectFeatureId);
        Assert.Equal(TenantEntitlementStatusConstants.Enabled, newClickCollect.EntitlementStatus);
        Assert.True(newClickCollect.IsEnabled);
        Assert.Equal("MANUAL", newClickCollect.SourceType);
        Assert.Equal(clickCollectFeatureId, newClickCollect.FeatureId);
        Assert.Equal(Now, newClickCollect.EffectiveFrom);
        Assert.Null(newClickCollect.RevokedAt);
        Assert.Equal(actorId, newClickCollect.CreatedByPlatformUserId);
        Assert.Equal(actorId, newClickCollect.UpdatedByPlatformUserId);

        await repository.ReplaceTenantEntitlementsAsync(
            tenantId,
            [onlineStoreFeatureId, clickCollectFeatureId],
            Now.AddMinutes(2),
            actorId,
            "Re-enable test",
            CancellationToken.None);

        var secondPassEntitlements = await dbContext.TenantFeatureEntitlements
            .Where(item => item.TenantId == tenantId)
            .ToListAsync();

        Assert.Equal(2, secondPassEntitlements.Count);
        var reenabledOnlineStore = Assert.Single(secondPassEntitlements, x => x.PlatformFeatureId == onlineStoreFeatureId);
        Assert.Equal(Guid.Parse("89999999-9999-4999-8999-999999999901"), reenabledOnlineStore.Id);
        Assert.Equal(TenantEntitlementStatusConstants.Enabled, reenabledOnlineStore.EntitlementStatus);
        Assert.True(reenabledOnlineStore.IsEnabled);
        Assert.Null(reenabledOnlineStore.RevokedAt);
        Assert.Null(reenabledOnlineStore.RevokedByPlatformUserId);
        Assert.Null(reenabledOnlineStore.RevokedReason);
        Assert.Equal(onlineStoreFeatureId, reenabledOnlineStore.FeatureId);

        Assert.Single(secondPassEntitlements, x => x.PlatformFeatureId == onlineStoreFeatureId);
        Assert.Single(secondPassEntitlements, x => x.PlatformFeatureId == clickCollectFeatureId);
    }

    private static async Task SeedPlanWithFeatureAsync(
        EPosDbContext dbContext,
        Guid planId,
        Guid featureId)
    {
        await SeedPlanWithFeaturesAsync(dbContext, planId, [featureId]);
    }

    private static async Task SeedPlanWithFeaturesAsync(
        EPosDbContext dbContext,
        Guid planId,
        IReadOnlyList<Guid> featureIds)
    {
        dbContext.SubscriptionPlans.Add(SubscriptionPlan.Create(
            planId,
            "LIFECYCLE",
            "Lifecycle Plan",
            SubscriptionPlanConstants.Status.Active,
            SubscriptionPlanConstants.BillingInterval.Monthly,
            49.99m,
            Now));

        var sortOrder = 1;
        foreach (var featureId in featureIds)
        {
            dbContext.PlatformFeatures.Add(PlatformFeature.Create(
                featureId,
                Guid.NewGuid(),
                sortOrder == 1 ? PlatformTenantFeatureCodes.OnlineStore : PlatformTenantFeatureCodes.ClickCollect,
                $"Feature {sortOrder}",
                "ACTIVE",
                Now,
                sortOrder));

            dbContext.SubscriptionPlanFeatures.Add(SubscriptionPlanFeature.CreateIncluded(
                Guid.NewGuid(),
                planId,
                featureId,
                sortOrder,
                Now));

            sortOrder++;
        }

        await dbContext.SaveChangesAsync();
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}



