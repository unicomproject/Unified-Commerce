using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformModulesCatalogRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid ActiveModuleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid InactiveModuleId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ActiveFeatureId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid InactiveFeatureId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public async Task GetActiveModulesAsync_ReturnsOnlyActiveModulesAndFeatures()
    {
        await using var dbContext = CreateDbContext();
        SeedCatalog(dbContext);

        IPlatformModulesCatalogRepository repository = new PlatformModulesCatalogRepository(dbContext);

        var modules = await repository.GetActiveModulesAsync(CancellationToken.None);

        Assert.Single(modules);
        Assert.Equal(ActiveModuleId, modules[0].Id);
        Assert.Equal("core_pos", modules[0].ModuleCode);
        Assert.Equal("ACTIVE", modules[0].Status);
        Assert.Single(modules[0].Features);
        Assert.Equal(ActiveFeatureId, modules[0].Features[0].Id);
        Assert.Equal("pos.sales", modules[0].Features[0].FeatureCode);
        Assert.Equal("ACTIVE", modules[0].Features[0].Status);
    }

    private static void SeedCatalog(EPosDbContext dbContext)
    {
        dbContext.PlatformModules.AddRange(
            PlatformModule.Create(
                ActiveModuleId,
                "core_pos",
                "Core POS",
                "Active module",
                "ACTIVE",
                10,
                Now),
            PlatformModule.Create(
                InactiveModuleId,
                "legacy_pos",
                "Legacy POS",
                "Inactive module",
                "INACTIVE",
                20,
                Now));

        dbContext.PlatformFeatures.AddRange(
            PlatformFeature.Create(
                ActiveFeatureId,
                ActiveModuleId,
                "pos.sales",
                "POS Sales",
                "ACTIVE",
                Now,
                sortOrder: 1),
            PlatformFeature.Create(
                InactiveFeatureId,
                ActiveModuleId,
                "pos.legacy",
                "Legacy POS Feature",
                "INACTIVE",
                Now,
                sortOrder: 2));

        dbContext.SaveChanges();
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}



