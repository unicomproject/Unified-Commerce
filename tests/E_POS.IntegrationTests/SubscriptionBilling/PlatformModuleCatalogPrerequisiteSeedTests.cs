using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.SubscriptionBilling;

public sealed class PlatformModuleCatalogPrerequisiteSeedTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 8, 9, 35, 0, TimeSpan.Zero);

    [Fact]
    public async Task PrerequisiteSeedApplicator_CreatesRequiredModulesAndFeatures()
    {
        await using var dbContext = CreateDbContext();

        await PlatformModuleCatalogPrerequisiteSeedApplicator.ApplyAsync(dbContext, Now);

        var modules = await dbContext.PlatformModules
            .AsNoTracking()
            .Where(item =>
                item.ModuleCode == PlatformModuleCatalogPrerequisiteSeedConstants.UserManagementModuleCode ||
                item.ModuleCode == PlatformModuleCatalogPrerequisiteSeedConstants.OutletTillCoreModuleCode)
            .ToListAsync();

        Assert.Equal(2, modules.Count);

        var features = await dbContext.PlatformFeatures
            .AsNoTracking()
            .Where(item =>
                item.FeatureCode == PlatformModuleCatalogPrerequisiteSeedConstants.UserAccountsFeatureCode ||
                item.FeatureCode == PlatformModuleCatalogPrerequisiteSeedConstants.OutletManagementFeatureCode ||
                item.FeatureCode == PlatformModuleCatalogPrerequisiteSeedConstants.TillManagementFeatureCode)
            .ToListAsync();

        Assert.Equal(3, features.Count);
        Assert.All(modules, item => Assert.True(item.IsCoreModule));
        Assert.All(features, item => Assert.True(item.IsCoreFeature));
    }

    [Fact]
    public async Task PrerequisiteSeedApplicator_IsIdempotent()
    {
        await using var dbContext = CreateDbContext();

        await PlatformModuleCatalogPrerequisiteSeedApplicator.ApplyAsync(dbContext, Now);
        await PlatformModuleCatalogPrerequisiteSeedApplicator.ApplyAsync(dbContext, Now);

        var featureCount = await dbContext.PlatformFeatures
            .AsNoTracking()
            .CountAsync(item =>
                item.FeatureCode == PlatformModuleCatalogPrerequisiteSeedConstants.UserAccountsFeatureCode ||
                item.FeatureCode == PlatformModuleCatalogPrerequisiteSeedConstants.OutletManagementFeatureCode ||
                item.FeatureCode == PlatformModuleCatalogPrerequisiteSeedConstants.TillManagementFeatureCode);

        Assert.Equal(3, featureCount);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
