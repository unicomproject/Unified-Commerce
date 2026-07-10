using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Services;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.Subscription.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class PlatformTenantWizardUsageCounterSeedTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 11, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodStart = new(2026, 7, 9, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SeedTenantCapacityCountersAsync_CreatesThreeTenantScopedCounters()
    {
        await using var dbContext = CreateDbContext();
        await SeedCanonicalLimitDefinitionsAsync(dbContext);
        var tenantId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await service.SeedTenantCapacityCountersAsync(
            tenantId,
            PeriodStart,
            periodEnd: null,
            maxOutlets: 7,
            maxUsers: 10,
            maxTills: 3,
            CancellationToken.None);

        var counters = await dbContext.TenantUsageCounters
            .AsNoTracking()
            .Where(counter => counter.TenantId == tenantId)
            .OrderBy(counter => counter.FeatureLimitDefinitionId)
            .ToListAsync();

        Assert.Equal(3, counters.Count);
        Assert.All(counters, counter => Assert.Equal(0m, counter.CurrentValue));
        Assert.All(counters, counter =>
            Assert.Equal(TenantUsageCounterAlignmentConstants.UsageScope.Tenant, counter.UsageScope));
        Assert.All(counters, counter => Assert.Null(counter.ScopeReferenceId));
        Assert.All(counters, counter => Assert.Equal(PeriodStart, counter.PeriodStart));
        Assert.All(counters, counter => Assert.Null(counter.PeriodEnd));
        Assert.All(counters, counter =>
            Assert.Equal(SubscriptionCatalogConstants.RecordStatus.Active, counter.Status));

        var outletsCounter = counters.Single(counter =>
            counter.FeatureLimitDefinitionId == SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId);
        var usersCounter = counters.Single(counter =>
            counter.FeatureLimitDefinitionId == SubscriptionCatalogLimitSeedConstants.MaxUsersLimitDefinitionId);
        var tillsCounter = counters.Single(counter =>
            counter.FeatureLimitDefinitionId == SubscriptionCatalogLimitSeedConstants.MaxTillsLimitDefinitionId);

        Assert.Equal(7m, outletsCounter.LimitValue);
        Assert.Equal(10m, usersCounter.LimitValue);
        Assert.Equal(3m, tillsCounter.LimitValue);
    }

    [Fact]
    public async Task SeedTenantCapacityCountersAsync_DualWritesLegacyFields()
    {
        await using var dbContext = CreateDbContext();
        await SeedCanonicalLimitDefinitionsAsync(dbContext);
        var tenantId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await service.SeedTenantCapacityCountersAsync(
            tenantId,
            PeriodStart,
            periodEnd: null,
            maxOutlets: 5,
            maxUsers: 5,
            maxTills: 5,
            CancellationToken.None);

        var outletsCounter = await dbContext.TenantUsageCounters
            .AsNoTracking()
            .SingleAsync(counter =>
                counter.TenantId == tenantId &&
                counter.FeatureLimitDefinitionId == SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId);

        var outletFeature = await dbContext.PlatformFeatures
            .AsNoTracking()
            .SingleAsync(feature =>
                feature.FeatureCode == SubscriptionCatalogLimitSeedConstants.OutletManagementFeatureCode);

        Assert.Equal(outletFeature.Id, outletsCounter.PlatformFeatureId);
        Assert.Equal(PeriodStart.ToString("O"), outletsCounter.UsagePeriodStart);
        Assert.Equal(0m, outletsCounter.UsedQuantity);
    }

    [Fact]
    public async Task SeedTenantCapacityCountersAsync_RepeatedUpsert_DoesNotCreateDuplicates()
    {
        await using var dbContext = CreateDbContext();
        await SeedCanonicalLimitDefinitionsAsync(dbContext);
        var tenantId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await service.SeedTenantCapacityCountersAsync(
            tenantId,
            PeriodStart,
            periodEnd: null,
            maxOutlets: 5,
            maxUsers: 5,
            maxTills: 5,
            CancellationToken.None);

        await service.SeedTenantCapacityCountersAsync(
            tenantId,
            PeriodStart,
            periodEnd: null,
            maxOutlets: 8,
            maxUsers: 9,
            maxTills: 4,
            CancellationToken.None);

        var counters = await dbContext.TenantUsageCounters
            .AsNoTracking()
            .Where(counter => counter.TenantId == tenantId)
            .ToListAsync();

        Assert.Equal(3, counters.Count);

        var outletsCounter = counters.Single(counter =>
            counter.FeatureLimitDefinitionId == SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId);
        Assert.Equal(8m, outletsCounter.LimitValue);
        Assert.Equal(0m, outletsCounter.CurrentValue);
    }

    [Fact]
    public async Task ValidateCanonicalCapacityLimitDefinitionsAsync_ThrowsWhenDefinitionMissing()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<MissingCanonicalCapacityLimitDefinitionException>(() =>
            service.ValidateCanonicalCapacityLimitDefinitionsAsync(CancellationToken.None));
    }

    private static TenantUsageCounterService CreateService(EPosDbContext dbContext)
    {
        var repository = new TenantUsageCounterRepository(dbContext);
        return new TenantUsageCounterService(repository, new FixedDateTimeProvider(Now));
    }

    private static void SeedTenant(EPosDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            "TEN-WIZ-COUNTERS",
            "ten-wiz-counters",
            "Wizard Counter Tenant",
            "active",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));
    }

    private static async Task SeedCanonicalLimitDefinitionsAsync(EPosDbContext dbContext)
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
        await SubscriptionCatalogLimitSeedApplicator.ApplyAsync(dbContext, Now);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }

    private sealed class FixedDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
