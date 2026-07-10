using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Services;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.Subscription.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.SubscriptionBilling;

public sealed class TenantUsageCounterServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 5, 50, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodStart = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Service_UpsertAndIncrement_PersistThroughRepository()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var limitId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId);
        SeedFeatureAndLimit(dbContext, featureId, limitId, "outlet_management", "max_outlets");
        await dbContext.SaveChangesAsync();

        var repository = new TenantUsageCounterRepository(dbContext);
        var service = new TenantUsageCounterService(repository, new FixedDateTimeProvider(Now));
        var key = new TenantUsageCounterKey(
            tenantId,
            limitId,
            TenantUsageCounterAlignmentConstants.UsageScope.Tenant,
            null,
            PeriodStart);

        await service.UpsertAsync(
            new TenantUsageCounterUpsertRequest(
                tenantId,
                limitId,
                TenantUsageCounterAlignmentConstants.UsageScope.Tenant,
                null,
                PeriodStart,
                CurrentValue: 1m,
                LimitValue: 5m),
            CancellationToken.None);

        var incremented = await service.IncrementCurrentValueAsync(key, 2m, CancellationToken.None);
        var read = await service.GetByKeyAsync(key, CancellationToken.None);

        Assert.Equal(3m, incremented.CurrentValue);
        Assert.Equal(3m, incremented.UsedQuantity);
        Assert.NotNull(read);
        Assert.Equal(incremented.Id, read!.Id);
    }

    [Fact]
    public async Task Service_SetCurrentValue_UpdatesBothQuantityFields()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var limitId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId);
        SeedFeatureAndLimit(dbContext, featureId, limitId, "user_accounts", "max_users");
        await dbContext.SaveChangesAsync();

        var repository = new TenantUsageCounterRepository(dbContext);
        var service = new TenantUsageCounterService(repository, new FixedDateTimeProvider(Now));
        var key = new TenantUsageCounterKey(
            tenantId,
            limitId,
            TenantUsageCounterAlignmentConstants.UsageScope.Tenant,
            null,
            PeriodStart);

        await service.UpsertAsync(
            new TenantUsageCounterUpsertRequest(
                tenantId,
                limitId,
                TenantUsageCounterAlignmentConstants.UsageScope.Tenant,
                null,
                PeriodStart,
                CurrentValue: 1m),
            CancellationToken.None);

        var updated = await service.SetCurrentValueAsync(key, 6m, CancellationToken.None);

        Assert.Equal(6m, updated.CurrentValue);
        Assert.Equal(6m, updated.UsedQuantity);
    }

    [Fact]
    public async Task SeedTenantCapacityCountersAsync_UsesCanonicalLimitDefinitions()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var limitId = SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId;
        SeedTenant(dbContext, tenantId);
        SeedFeatureAndLimit(dbContext, featureId, limitId, "outlet_management", "max_outlets");
        SeedFeatureAndLimit(
            dbContext,
            Guid.NewGuid(),
            SubscriptionCatalogLimitSeedConstants.MaxUsersLimitDefinitionId,
            "user_accounts",
            "max_users");
        SeedFeatureAndLimit(
            dbContext,
            Guid.NewGuid(),
            SubscriptionCatalogLimitSeedConstants.MaxTillsLimitDefinitionId,
            "till_management",
            "max_tills");
        await dbContext.SaveChangesAsync();

        var repository = new TenantUsageCounterRepository(dbContext);
        var service = new TenantUsageCounterService(repository, new FixedDateTimeProvider(Now));

        await service.SeedTenantCapacityCountersAsync(
            tenantId,
            PeriodStart,
            periodEnd: null,
            maxOutlets: 4,
            maxUsers: 6,
            maxTills: 2,
            CancellationToken.None);

        var counters = await dbContext.TenantUsageCounters
            .AsNoTracking()
            .Where(counter => counter.TenantId == tenantId)
            .ToListAsync();

        Assert.Equal(3, counters.Count);
    }

    private static void SeedTenant(EPosDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            "TEN-SVC",
            "ten-svc",
            "Service Tenant",
            "active",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));
    }

    private static void SeedFeatureAndLimit(
        EPosDbContext dbContext,
        Guid featureId,
        Guid limitId,
        string featureCode,
        string limitCode)
    {
        var moduleId = Guid.NewGuid();
        dbContext.PlatformModules.Add(PlatformModule.Create(
            moduleId,
            featureCode,
            featureCode,
            description: null,
            status: "active",
            sortOrder: 0,
            now: Now));

        dbContext.PlatformFeatures.Add(PlatformFeature.Create(
            featureId,
            moduleId,
            featureCode,
            featureCode,
            "active",
            Now));

        dbContext.FeatureLimitDefinitions.Add(FeatureLimitDefinition.Create(
            limitId,
            featureId,
            limitCode,
            limitCode,
            5m,
            Now));
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
