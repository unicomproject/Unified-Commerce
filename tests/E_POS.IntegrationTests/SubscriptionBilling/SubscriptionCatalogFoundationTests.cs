using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.SubscriptionBilling;

public sealed class SubscriptionCatalogFoundationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 8, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task SeedApplicator_PopulatesSecondBrainCatalogColumns()
    {
        await using var dbContext = CreateDbContext();
        await SubscriptionBillingCatalogSeedApplicator.ApplyAsync(dbContext, Now);

        var module = await dbContext.PlatformModules
            .SingleAsync(x => x.ModuleCode == "core_commerce");

        Assert.Equal("core_commerce", module.ModuleKey);
        Assert.Equal("Core Commerce", module.ModuleName);
        Assert.True(module.IsCoreModule);

        var feature = await dbContext.PlatformFeatures
            .SingleAsync(x => x.FeatureCode == "online_store");

        Assert.Equal("online_store", feature.FeatureKey);
        Assert.Equal("Online Store", feature.FeatureName);
        Assert.Equal("Enable tenant online store channel.", feature.Description);
    }

    [Fact]
    public async Task SubscriptionPlanCreate_SyncsLegacyAndSecondBrainCommercialFields()
    {
        await using var dbContext = CreateDbContext();
        var actorId = Guid.NewGuid();

        var plan = SubscriptionPlan.CreateDraft(
            Guid.NewGuid(),
            "STARTER-001",
            "Starter Plan",
            "Starter subscription plan.",
            SubscriptionPlanConstants.BillingInterval.Monthly,
            "LKR",
            4999.50m,
            1,
            5,
            2,
            Now,
            actorId,
            trialDays: 14,
            isPublic: true,
            isCustomPlan: false);

        dbContext.SubscriptionPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.SubscriptionPlans.SingleAsync(x => x.Id == plan.Id);

        Assert.Equal("Starter Plan", saved.Name);
        Assert.Equal("Starter Plan", saved.PlanName);
        Assert.Equal(SubscriptionPlanConstants.BillingInterval.Monthly, saved.BillingInterval);
        Assert.Equal(SubscriptionPlanConstants.BillingInterval.Monthly, saved.BillingCycle);
        Assert.Equal("LKR", saved.BaseCurrency);
        Assert.Equal("LKR", saved.BaseCurrencyCode);
        Assert.Equal(4999.50m, saved.PriceAmount);
        Assert.Equal(4999.50m, saved.BasePrice);
        Assert.Equal(14, saved.TrialDays);
        Assert.True(saved.IsPublic);
        Assert.False(saved.IsCustomPlan);
        Assert.Equal(actorId, saved.CreatedByPlatformUserId);
        Assert.Equal(1, saved.MaxOutlets);
        Assert.Equal(5, saved.MaxUsers);
        Assert.Equal(2, saved.MaxTills);
    }

    [Fact]
    public async Task FeatureLimitDefinitionCreate_PopulatesSecondBrainLimitColumns()
    {
        await using var dbContext = CreateDbContext();
        await SubscriptionBillingCatalogSeedApplicator.ApplyAsync(dbContext, Now);

        var feature = await dbContext.PlatformFeatures
            .SingleAsync(x => x.FeatureCode == "online_store");

        var limit = FeatureLimitDefinition.Create(
            Guid.NewGuid(),
            feature.Id,
            "max_outlets",
            "Maximum Outlets",
            10m,
            Now,
            SubscriptionCatalogConstants.LimitValueType.Integer,
            "outlet");

        dbContext.FeatureLimitDefinitions.Add(limit);
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.FeatureLimitDefinitions.SingleAsync(x => x.Id == limit.Id);

        Assert.Equal("max_outlets", saved.LimitCode);
        Assert.Equal("max_outlets", saved.LimitKey);
        Assert.Equal("Maximum Outlets", saved.LimitName);
        Assert.Equal(10m, saved.DefaultLimitValue);
        Assert.Equal(SubscriptionCatalogConstants.LimitValueType.Integer, saved.ValueType);
        Assert.Equal("outlet", saved.UnitCode);
        Assert.True(saved.IsHardLimit);
        Assert.Equal(SubscriptionCatalogConstants.RecordStatus.Active, saved.Status);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
