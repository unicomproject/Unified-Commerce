using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformTenantRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetTenantsAsync_WithSeededData_AppliesSearchStatusPlanAndPagination()
    {
        await using var dbContext = CreateDbContext();
        var planId = Guid.Parse("77777777-7777-4777-8777-777777777701");
        var tenantOneId = Guid.Parse("11111111-1111-4111-8111-111111111101");
        var tenantTwoId = Guid.Parse("11111111-1111-4111-8111-111111111102");
        var tenantThreeId = Guid.Parse("11111111-1111-4111-8111-111111111103");

        await SeedAsync(dbContext, planId, tenantOneId, tenantTwoId, tenantThreeId);

        IPlatformTenantRepository repository = new PlatformTenantRepository(dbContext);

        var filtered = await repository.GetTenantsAsync(new PlatformTenantListQuery
        {
            Search = "Active",
            Status = "active",
            BillingStatus = "ACTIVE",
            PlanId = planId,
            PageNumber = 1,
            PageSize = 10,
            SortBy = "name",
            SortDirection = "asc"
        }, CancellationToken.None);

        Assert.Equal(1, filtered.TotalCount);
        Assert.Single(filtered.Items);
        Assert.Equal("TEN-001", filtered.Items[0].Code);
        Assert.Equal(planId, filtered.Items[0].Subscription!.PlanId);
        Assert.Equal(1, filtered.Items[0].OutletCount);
        Assert.Equal(1, filtered.Items[0].TillCount);
        Assert.Equal(1, filtered.Items[0].UserCount);
        Assert.True(filtered.Items[0].OnlineStoreEnabled);
    }

    [Fact]
    public async Task GetSummaryAsync_WithSeededData_ReturnsAggregateCounts()
    {
        await using var dbContext = CreateDbContext();
        var planId = Guid.Parse("77777777-7777-4777-8777-777777777702");
        await SeedAsync(
            dbContext,
            planId,
            Guid.Parse("11111111-1111-4111-8111-111111111104"),
            Guid.Parse("11111111-1111-4111-8111-111111111105"),
            Guid.Parse("11111111-1111-4111-8111-111111111106"));

        IPlatformTenantRepository repository = new PlatformTenantRepository(dbContext);

        var summary = await repository.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(3, summary.TotalTenants);
        Assert.Equal(2, summary.ActiveTenants);
        Assert.Equal(1, summary.SuspendedTenants);
        Assert.Equal(1, summary.TrialTenants);
        Assert.Equal(1, summary.PendingBillingCount);
        Assert.Equal(1, summary.TotalOutlets);
        Assert.Equal(1, summary.TotalTills);
    }

    [Fact]
    public async Task GetFilterOptionsAsync_WithSeededData_ReturnsDistinctValues()
    {
        await using var dbContext = CreateDbContext();
        var planId = Guid.Parse("77777777-7777-4777-8777-777777777703");
        await SeedAsync(
            dbContext,
            planId,
            Guid.Parse("11111111-1111-4111-8111-111111111107"),
            Guid.Parse("11111111-1111-4111-8111-111111111108"),
            Guid.Parse("11111111-1111-4111-8111-111111111109"));

        IPlatformTenantRepository repository = new PlatformTenantRepository(dbContext);

        var options = await repository.GetFilterOptionsAsync(CancellationToken.None);

        Assert.Contains("active", options.Statuses);
        Assert.Contains("ACTIVE", options.BillingStatuses);
        Assert.Contains("STANDARD", options.OperatingModes);
        Assert.Contains(options.Plans, plan => plan.Id == planId);
    }

    [Fact]
    public async Task GetTenantsAsync_WithEmptyDatabase_ReturnsEmptyList()
    {
        await using var dbContext = CreateDbContext();
        IPlatformTenantRepository repository = new PlatformTenantRepository(dbContext);

        var list = await repository.GetTenantsAsync(new PlatformTenantListQuery(), CancellationToken.None);
        var summary = await repository.GetSummaryAsync(CancellationToken.None);
        var options = await repository.GetFilterOptionsAsync(CancellationToken.None);

        Assert.Empty(list.Items);
        Assert.Equal(0, list.TotalCount);
        Assert.Equal(0, summary.TotalTenants);
        Assert.Empty(options.Statuses);
        Assert.Empty(options.Plans);
    }

    [Fact]
    public async Task GetTenantDetailAsync_WithSeededTenant_ReturnsDetailCountsAndSubscription()
    {
        await using var dbContext = CreateDbContext();
        var planId = Guid.Parse("77777777-7777-4777-8777-777777777711");
        var tenantId = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var onlineStoreFeatureId = Guid.Parse("88888888-8888-4888-8888-888888888801");
        await SeedAsync(dbContext, planId, tenantId, Guid.NewGuid(), Guid.NewGuid());

        IPlatformTenantRepository repository = new PlatformTenantRepository(dbContext);

        var detail = await repository.GetTenantDetailAsync(tenantId, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal("TEN-001", detail!.Code);
        Assert.Equal(planId, detail.Subscription!.PlanId);
        Assert.Equal("ACTIVE", detail.Subscription.SubscriptionStatus);
        Assert.Null(detail.Subscription.StartsAt);
        Assert.Equal(1, detail.UserCount);
        Assert.True(detail.OnlineStoreEnabled);
        Assert.Equal([onlineStoreFeatureId], detail.EnabledFeatureIds);
        Assert.Equal(["online_store"], detail.EnabledFeatureCodes);
        Assert.Null(detail.Profile);
        Assert.Null(detail.PrimaryAddress);
    }

    [Fact]
    public async Task GetTenantDetailAsync_WithMissingSubscriptionAndProfile_ReturnsNullOptionalSections()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            "TEN-999",
            "ten-999",
            "Minimal Tenant",
            "active",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));
        await dbContext.SaveChangesAsync();

        IPlatformTenantRepository repository = new PlatformTenantRepository(dbContext);

        var detail = await repository.GetTenantDetailAsync(tenantId, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Null(detail!.Subscription);
        Assert.Null(detail.Profile);
        Assert.Null(detail.PrimaryAddress);
        Assert.Equal(0, detail.UserCount);
    }

    [Fact]
    public async Task GetTenantDetailAsync_WhenTenantMissing_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        IPlatformTenantRepository repository = new PlatformTenantRepository(dbContext);

        var detail = await repository.GetTenantDetailAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(detail);
    }

    [Fact]
    public async Task GetEntitlementOptionsAsync_WithSeededTenant_ReturnsPlansCatalogAndEnabledFeatures()
    {
        await using var dbContext = CreateDbContext();
        var planId = Guid.Parse("77777777-7777-4777-8777-777777777711");
        var tenantId = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var onlineStoreFeatureId = Guid.Parse("88888888-8888-4888-8888-888888888801");
        await SeedAsync(dbContext, planId, tenantId, Guid.NewGuid(), Guid.NewGuid());

        IPlatformTenantRepository repository = new PlatformTenantRepository(dbContext);

        var options = await repository.GetEntitlementOptionsAsync(tenantId, CancellationToken.None);

        Assert.NotNull(options);
        Assert.Equal(tenantId, options!.TenantId);
        Assert.Equal(planId, options.CurrentSubscriptionPlanId);
        Assert.Equal("STARTER", options.CurrentSubscriptionPlanCode);
        Assert.Equal([onlineStoreFeatureId], options.EnabledFeatureIds);
        Assert.Equal(["online_store"], options.EnabledFeatureCodes);
        Assert.Single(options.Plans);
        Assert.Equal([onlineStoreFeatureId], options.Plans[0].IncludedFeatureIds);
        Assert.Equal(["online_store"], options.Plans[0].IncludedFeatureCodes);
        Assert.Single(options.CatalogModules);
        Assert.Single(options.CatalogModules[0].Features);
        Assert.Equal("online_store", options.CatalogModules[0].Features[0].Code);
    }

    [Fact]
    public async Task TenantReads_ExcludeRevokedEntitlementsEvenIfLegacyStatusIsEnabled()
    {
        await using var dbContext = CreateDbContext();
        var planId = Guid.Parse("77777777-7777-4777-8777-777777777712");
        var tenantId = Guid.Parse("11111111-1111-4111-8111-111111111112");
        await SeedAsync(dbContext, planId, tenantId, Guid.NewGuid(), Guid.NewGuid());

        var entitlement = await dbContext.TenantFeatureEntitlements
            .SingleAsync(item => item.TenantId == tenantId);
        entitlement.Disable(Now.AddMinutes(1), null, "revoked for test", null);
        dbContext.Entry(entitlement).Property(nameof(TenantFeatureEntitlement.EntitlementStatus)).CurrentValue = TenantEntitlementStatusConstants.Enabled;
        await dbContext.SaveChangesAsync();

        IPlatformTenantRepository repository = new PlatformTenantRepository(dbContext);
        var detail = await repository.GetTenantDetailAsync(tenantId, CancellationToken.None);
        var options = await repository.GetEntitlementOptionsAsync(tenantId, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.NotNull(options);
        Assert.False(detail!.OnlineStoreEnabled);
        Assert.Empty(detail.EnabledFeatureIds);
        Assert.Empty(options!.EnabledFeatureIds);
    }

    [Fact]
    public async Task GetEntitlementOptionsAsync_WhenTenantMissing_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        IPlatformTenantRepository repository = new PlatformTenantRepository(dbContext);

        var options = await repository.GetEntitlementOptionsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(options);
    }

    [Fact]
    public async Task GetCreateOptionsAsync_WithAddonMissingLimitRow_DoesNotThrow()
    {
        await using var dbContext = CreateDbContext();
        var planId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var limitDefinitionId = Guid.NewGuid();
        var addonWithLimitId = Guid.NewGuid();
        var addonWithoutLimitId = Guid.NewGuid();

        dbContext.SubscriptionPlans.Add(SubscriptionPlan.Create(
            planId,
            "WIZARD_PLAN",
            "Wizard Plan",
            SubscriptionPlanConstants.Status.Active,
            "MONTHLY",
            49.99m,
            Now));

        dbContext.PlatformModules.Add(PlatformModule.Create(
            moduleId,
            "wizard_module",
            "Wizard Module",
            "Wizard module",
            "ACTIVE",
            1,
            Now));

        dbContext.PlatformFeatures.Add(PlatformFeature.Create(
            featureId,
            moduleId,
            "outlet_management",
            "Outlet Management",
            "ACTIVE",
            Now));

        dbContext.FeatureLimitDefinitions.Add(FeatureLimitDefinition.Create(
            limitDefinitionId,
            featureId,
            "MAX_OUTLETS",
            "Max Outlets",
            1m,
            Now));

        dbContext.SubscriptionAddons.AddRange(
            SubscriptionAddon.Create(
                addonWithLimitId,
                "ADDON_WITH_LIMIT",
                "Addon With Limit",
                "ACTIVE",
                10m,
                Now),
            SubscriptionAddon.Create(
                addonWithoutLimitId,
                "ADDON_WITHOUT_LIMIT",
                "Addon Without Limit",
                "ACTIVE",
                5m,
                Now));

        dbContext.SubscriptionAddonLimits.Add(SubscriptionAddonLimit.Create(
            Guid.NewGuid(),
            addonWithLimitId,
            limitDefinitionId,
            2m,
            Now));

        await dbContext.SaveChangesAsync();

        IPlatformTenantRepository repository = new PlatformTenantRepository(dbContext);

        var options = await repository.GetCreateOptionsAsync(CancellationToken.None);

        Assert.Single(options.Plans);
        Assert.Equal(2, options.Addons.Count);

        var addonWithLimit = options.Addons.Single(addon => addon.AddonCode == "ADDON_WITH_LIMIT");
        Assert.Equal(2, addonWithLimit.LimitIncrementByKey["max_outlets"]);

        var addonWithoutLimit = options.Addons.Single(addon => addon.AddonCode == "ADDON_WITHOUT_LIMIT");
        Assert.Empty(addonWithoutLimit.LimitIncrementByKey);
    }

    private static async Task SeedAsync(
        EPosDbContext dbContext,
        Guid planId,
        Guid tenantOneId,
        Guid tenantTwoId,
        Guid tenantThreeId)
    {
        var onlineStoreFeatureId = Guid.Parse("88888888-8888-4888-8888-888888888801");
        var moduleId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");

        dbContext.SubscriptionPlans.Add(SubscriptionPlan.Create(
            planId,
            "STARTER",
            "Starter Plan",
            SubscriptionPlanConstants.Status.Active,
            "MONTHLY",
            49.99m,
            Now));

        dbContext.PlatformModules.Add(PlatformModule.Create(
            moduleId,
            "commerce",
            "Commerce",
            "Commerce module",
            "ACTIVE",
            1,
            Now));

        dbContext.PlatformFeatures.Add(PlatformFeature.Create(
            onlineStoreFeatureId,
            moduleId,
            PlatformTenantFeatureCodes.OnlineStore,
            "Online Store",
            "ACTIVE",
            Now,
            1));

        dbContext.SubscriptionPlanFeatures.Add(SubscriptionPlanFeature.CreateIncluded(
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
            planId,
            onlineStoreFeatureId,
            1,
            Now));

        dbContext.Tenants.AddRange(
            Tenant.Create(tenantOneId, "TEN-001", "ten-001", "Active Tenant", "active", "LKR", "Asia/Colombo", null, null, Now.AddDays(-3)),
            Tenant.Create(tenantTwoId, "TEN-002", "ten-002", "Suspended Tenant", "suspended", "LKR", "Asia/Colombo", null, null, Now.AddDays(-2)),
            Tenant.Create(tenantThreeId, "TEN-003", "ten-003", "Trial Tenant", "active", "LKR", "Asia/Colombo", null, null, Now.AddDays(-1)));

        dbContext.TenantSubscriptions.AddRange(
            TenantSubscription.Create(
                Guid.Parse("22222222-2222-4222-8222-222222222201"),
                tenantOneId,
                planId,
                "ACTIVE",
                Now),
            TenantSubscription.Create(
                Guid.Parse("22222222-2222-4222-8222-222222222202"),
                tenantThreeId,
                planId,
                "TRIAL",
                Now),
            TenantSubscription.Create(
                Guid.Parse("22222222-2222-4222-8222-222222222203"),
                tenantTwoId,
                planId,
                "PAST_DUE",
                Now));

        dbContext.TenantFeatureEntitlements.Add(TenantFeatureEntitlement.Create(
            Guid.Parse("99999999-9999-4999-8999-999999999901"),
            tenantOneId,
            onlineStoreFeatureId,
            "ENABLED",
            Now));

        dbContext.Outlets.Add(Outlet.Create(
            Guid.Parse("33333333-3333-4333-8333-333333333301"),
            tenantOneId,
            "Main Outlet",
            "OUT-001",
            "ACTIVE",
            "STORE",
            true,
            null,
            null,
            Now));

        dbContext.Tills.Add(Till.Create(
            Guid.Parse("44444444-4444-4444-8444-444444444401"),
            tenantOneId,
            Guid.Parse("33333333-3333-4333-8333-333333333301"),
            "Front Till",
            "TILL-001",
            "ACTIVE",
            Now));

        dbContext.TenantUsers.Add(TenantUser.Create(
            Guid.Parse("55555555-5555-4555-8555-555555555501"),
            tenantOneId,
            "user@tenant.test",
            "User Test",
            null,
            null,
            "hash",
            "salt",
            "ACTIVE",
            "standard",
            "system",
            "default",
            Now));

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



