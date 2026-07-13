using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Infrastructure.Modules.Platform.Subscription.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.SubscriptionBilling;

public sealed class PlatformSubscriptionPlanRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetCatalogAsync_ReadsSeededCommercialModulesAndFeatures()
    {
        await using var dbContext = CreateDbContext();
        await SubscriptionBillingCatalogSeedApplicator.ApplyAsync(dbContext, Now);

        IPlatformSubscriptionPlanRepository repository = new PlatformSubscriptionPlanRepository(dbContext);

        var catalog = await repository.GetCatalogAsync(CancellationToken.None);

        Assert.Single(catalog.Modules);
        Assert.Equal(3, catalog.Modules[0].Features.Count);
    }

    [Fact]
    public async Task CreateAndPublishPlan_UpdatesStatusAndFeatureCount()
    {
        await using var dbContext = CreateDbContext();
        await SubscriptionBillingCatalogSeedApplicator.ApplyAsync(dbContext, Now);

        IPlatformSubscriptionPlanRepository repository = new PlatformSubscriptionPlanRepository(dbContext);
        var planId = Guid.NewGuid();

        var plan = SubscriptionPlan.CreateDraft(
            planId,
            "INTEGRATION_TEST",
            "Integration Test Plan",
            "Integration test plan.",
            SubscriptionPlanConstants.BillingInterval.Monthly,
            SubscriptionPlanConstants.DefaultBaseCurrency,
            99.99m,
            maxOutlets: 2,
            maxUsers: 5,
            maxTills: 3,
            Now);

        await repository.AddPlanAsync(plan, CancellationToken.None);

        await repository.ReplacePlanFeaturesAsync(
            planId,
            [SubscriptionBillingCatalogSeedConstants.OnlineStoreFeatureId],
            Now,
            CancellationToken.None);

        var draft = await repository.GetPlanByIdAsync(
            planId,
            new SubscriptionPlanPermissionFlags(true, true, true, true, true, true),
            CancellationToken.None);

        Assert.NotNull(draft);
        Assert.Equal(SubscriptionPlanConstants.Status.Draft, draft!.Status);
        Assert.Equal(1, draft.FeatureCount);

        var entity = await repository.GetPlanEntityByIdAsync(planId, CancellationToken.None);
        entity!.Publish(Now);
        await repository.SaveChangesAsync(CancellationToken.None);

        var published = await repository.GetPlanByIdAsync(
            planId,
            new SubscriptionPlanPermissionFlags(true, true, true, true, true, true),
            CancellationToken.None);

        Assert.Equal(SubscriptionPlanConstants.Status.Active, published!.Status);
    }

    [Fact]
    public async Task GetPlanDetailByIdAsync_ReturnsConfiguredLimitsAndGroupedFeatures()
    {
        await using var dbContext = CreateDbContext();
        await SubscriptionBillingCatalogSeedApplicator.ApplyAsync(dbContext, Now);

        IPlatformSubscriptionPlanRepository repository = new PlatformSubscriptionPlanRepository(dbContext);
        var planId = Guid.NewGuid();
        var plan = SubscriptionPlan.CreateDraft(
            planId,
            "DETAIL_TEST",
            "Detail Test Plan",
            "Detail endpoint contract.",
            SubscriptionPlanConstants.BillingInterval.Monthly,
            SubscriptionPlanConstants.DefaultBaseCurrency,
            49.99m,
            2,
            5,
            3,
            Now);

        await repository.AddPlanAsync(plan, CancellationToken.None);
        await repository.ReplacePlanFeaturesAsync(
            planId,
            [SubscriptionBillingCatalogSeedConstants.OnlineStoreFeatureId],
            Now,
            CancellationToken.None);
        var limitDefinitionId = Guid.NewGuid();
        dbContext.FeatureLimitDefinitions.Add(FeatureLimitDefinition.Create(
            limitDefinitionId,
            SubscriptionBillingCatalogSeedConstants.OnlineStoreFeatureId,
            "MAX_ORDERS",
            "Maximum orders",
            100m,
            Now));
        dbContext.SubscriptionPlanFeatureLimits.Add(SubscriptionPlanFeatureLimit.Create(
            Guid.NewGuid(),
            planId,
            limitDefinitionId,
            250m,
            isUnlimited: false,
            now: Now));
        await dbContext.SaveChangesAsync();

        var detail = await repository.GetPlanDetailByIdAsync(
            planId,
            new SubscriptionPlanPermissionFlags(true, true, true, true, true, true),
            CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal(Now, detail!.CreatedAt);
        Assert.Equal("fixed", detail.PricingModel);
        Assert.Single(detail.Limits);
        Assert.Equal("Maximum orders", detail.Limits[0].Name);
        Assert.Single(detail.Modules);
        Assert.Single(detail.Modules[0].Features);
        Assert.Equal("online_store", detail.Modules[0].Features[0].Code);

        var copyId = Guid.NewGuid();
        await repository.AddPlanAsync(SubscriptionPlan.CreateDraft(
            copyId, "DETAIL_COPY", "Detail Copy", null,
            SubscriptionPlanConstants.BillingInterval.Monthly,
            SubscriptionPlanConstants.DefaultBaseCurrency,
            49.99m, 2, 5, 3, Now), CancellationToken.None);
        await repository.CopyPlanConfigurationAsync(planId, copyId, Now, CancellationToken.None);

        Assert.Single(await dbContext.SubscriptionPlanFeatures.Where(item => item.SubscriptionPlanId == copyId).ToListAsync());
        Assert.Single(await dbContext.SubscriptionPlanFeatureLimits.Where(item => item.SubscriptionPlanId == copyId).ToListAsync());
    }

    [Fact]
    public async Task GetPlansAsync_FiltersByDraftStatus()
    {
        await using var dbContext = CreateDbContext();

        dbContext.SubscriptionPlans.AddRange(
            SubscriptionPlan.CreateDraft(
                Guid.NewGuid(),
                "DRAFT_ONE",
                "Draft One",
                null,
                SubscriptionPlanConstants.BillingInterval.Monthly,
                SubscriptionPlanConstants.DefaultBaseCurrency,
                10m,
                1,
                1,
                1,
                Now),
            SubscriptionPlan.Create(
                Guid.NewGuid(),
                "ACTIVE_ONE",
                "Active One",
                SubscriptionPlanConstants.Status.Active,
                SubscriptionPlanConstants.BillingInterval.Monthly,
                20m,
                Now));

        await dbContext.SaveChangesAsync();

        IPlatformSubscriptionPlanRepository repository = new PlatformSubscriptionPlanRepository(dbContext);

        var response = await repository.GetPlansAsync(
            new Application.Modules.Platform.Subscription.Dtos.SubscriptionPlanListQuery
            {
                Status = SubscriptionPlanConstants.Status.Draft
            },
            new SubscriptionPlanPermissionFlags(true, true, true, true, true, true),
            CancellationToken.None);

        Assert.Equal(1, response.TotalCount);
        Assert.Equal("DRAFT_ONE", response.Items[0].PlanCode);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}




