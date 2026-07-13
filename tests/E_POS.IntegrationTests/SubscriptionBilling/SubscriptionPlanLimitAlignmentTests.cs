using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Infrastructure.Modules.Platform.Subscription.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.SubscriptionBilling;

public sealed class SubscriptionPlanLimitAlignmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 8, 9, 40, 0, TimeSpan.Zero);

    [Fact]
    public async Task LimitSeedApplicator_CreatesCanonicalMaxLimitDefinitions()
    {
        await using var dbContext = CreateDbContext();
        await SeedRequiredFeaturesAsync(dbContext);

        await SubscriptionCatalogLimitSeedApplicator.ApplyAsync(dbContext, Now);

        var limits = await dbContext.FeatureLimitDefinitions
            .AsNoTracking()
            .Where(item =>
                item.LimitKey == SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitKey ||
                item.LimitKey == SubscriptionCatalogLimitSeedConstants.MaxUsersLimitKey ||
                item.LimitKey == SubscriptionCatalogLimitSeedConstants.MaxTillsLimitKey)
            .ToListAsync();

        Assert.Equal(3, limits.Count);
        Assert.Contains(limits, item => item.Id == SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId);
        Assert.Contains(limits, item => item.Id == SubscriptionCatalogLimitSeedConstants.MaxUsersLimitDefinitionId);
        Assert.Contains(limits, item => item.Id == SubscriptionCatalogLimitSeedConstants.MaxTillsLimitDefinitionId);
    }

    [Fact]
    public async Task LimitSeedApplicator_ThrowsWhenRequiredFeatureMissing()
    {
        await using var dbContext = CreateDbContext();

        await Assert.ThrowsAsync<SubscriptionCatalogLimitSeedApplicator.MissingRequiredFeatureException>(
            () => SubscriptionCatalogLimitSeedApplicator.ApplyAsync(dbContext, Now));
    }

    [Fact]
    public async Task UpsertLegacyPlanLimitsAsync_WritesPlanFeatureLimitRows()
    {
        await using var dbContext = CreateDbContext();
        await SeedRequiredFeaturesAsync(dbContext);
        await SubscriptionCatalogLimitSeedApplicator.ApplyAsync(dbContext, Now);

        var planId = Guid.NewGuid();
        dbContext.SubscriptionPlans.Add(SubscriptionPlan.CreateDraft(
            planId,
            "LIMIT-PLAN",
            "Limit Plan",
            "Limit plan.",
            SubscriptionPlanConstants.BillingInterval.Monthly,
            "LKR",
            100m,
            2,
            5,
            3,
            Now));

        await dbContext.SaveChangesAsync();

        IPlatformSubscriptionPlanRepository repository = new PlatformSubscriptionPlanRepository(dbContext);
        await repository.UpsertLegacyPlanLimitsAsync(planId, 2, 5, 3, Now, CancellationToken.None);

        var limits = await repository.GetPlanLimitValuesByKeyAsync(planId, CancellationToken.None);

        Assert.Equal(2m, limits[SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitKey]);
        Assert.Equal(5m, limits[SubscriptionCatalogLimitSeedConstants.MaxUsersLimitKey]);
        Assert.Equal(3m, limits[SubscriptionCatalogLimitSeedConstants.MaxTillsLimitKey]);
    }

    [Fact]
    public async Task GetPlanById_StillReturnsLegacyMaxFieldsAfterLimitSync()
    {
        await using var dbContext = CreateDbContext();
        await SeedRequiredFeaturesAsync(dbContext);
        await SubscriptionCatalogLimitSeedApplicator.ApplyAsync(dbContext, Now);

        var planId = Guid.NewGuid();
        dbContext.SubscriptionPlans.Add(SubscriptionPlan.CreateDraft(
            planId,
            "LEGACY-LIMIT-PLAN",
            "Legacy Limit Plan",
            "Legacy limit plan.",
            SubscriptionPlanConstants.BillingInterval.Monthly,
            "LKR",
            250m,
            4,
            8,
            6,
            Now));

        await dbContext.SaveChangesAsync();

        IPlatformSubscriptionPlanRepository repository = new PlatformSubscriptionPlanRepository(dbContext);
        await repository.UpsertLegacyPlanLimitsAsync(planId, 4, 8, 6, Now, CancellationToken.None);

        var plan = await repository.GetPlanByIdAsync(
            planId,
            new SubscriptionPlanPermissionFlags(true, true, true, true, true, true),
            CancellationToken.None);

        Assert.NotNull(plan);
        Assert.Equal(4, plan!.MaxOutlets);
        Assert.Equal(8, plan.MaxUsers);
        Assert.Equal(6, plan.MaxTills);
    }

    [Fact]
    public async Task AddonLimitSeed_CreatesIncrementRowsWhenCapacityAddonsExist()
    {
        await using var dbContext = CreateDbContext();
        await SeedRequiredFeaturesAsync(dbContext);
        await SubscriptionCatalogLimitSeedApplicator.ApplyAsync(dbContext, Now);

        var addonId = Guid.NewGuid();
        dbContext.SubscriptionAddons.Add(SubscriptionAddon.Create(
            addonId,
            SubscriptionCatalogLimitSeedConstants.ExtraOutletAddonCode,
            "Extra Outlet",
            "ACTIVE",
            500m,
            Now));

        await dbContext.SaveChangesAsync();
        await SubscriptionCatalogLimitSeedApplicator.ApplyAsync(dbContext, Now);

        var addonLimit = await dbContext.SubscriptionAddonLimits
            .AsNoTracking()
            .SingleAsync(item =>
                item.SubscriptionAddonId == addonId &&
                item.FeatureLimitDefinitionId == SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId);

        Assert.Equal(1m, addonLimit.IncrementValue);
    }

    private static async Task SeedRequiredFeaturesAsync(EPosDbContext dbContext)
    {
        var moduleId = Guid.NewGuid();
        dbContext.PlatformModules.Add(PlatformModule.Create(
            moduleId,
            "limit_test_module",
            "Limit Test Module",
            "Limit test module.",
            PlatformAuthConstants.ActiveStatus,
            1,
            Now));

        dbContext.PlatformFeatures.AddRange(
            PlatformFeature.Create(
                Guid.NewGuid(),
                moduleId,
                SubscriptionCatalogLimitSeedConstants.OutletManagementFeatureCode,
                "Outlet Management",
                PlatformAuthConstants.ActiveStatus,
                Now),
            PlatformFeature.Create(
                Guid.NewGuid(),
                moduleId,
                SubscriptionCatalogLimitSeedConstants.UserAccountsFeatureCode,
                "User Accounts",
                PlatformAuthConstants.ActiveStatus,
                Now),
            PlatformFeature.Create(
                Guid.NewGuid(),
                moduleId,
                SubscriptionCatalogLimitSeedConstants.TillManagementFeatureCode,
                "Till Management",
                PlatformAuthConstants.ActiveStatus,
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
