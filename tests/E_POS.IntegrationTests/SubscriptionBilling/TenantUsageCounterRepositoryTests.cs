using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.Subscription.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.SubscriptionBilling;

public sealed class TenantUsageCounterRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 5, 45, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodStart = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UpsertAsync_CreatesCounterWithSecondBrainAndLegacyFields()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var limitId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId);
        SeedFeatureAndLimit(dbContext, featureId, limitId, "outlet_management", "max_outlets");
        await dbContext.SaveChangesAsync();

        ITenantUsageCounterRepository repository = new TenantUsageCounterRepository(dbContext);

        var counter = await repository.UpsertAsync(
            new TenantUsageCounterUpsertRequest(
                tenantId,
                limitId,
                TenantUsageCounterAlignmentConstants.UsageScope.Tenant,
                null,
                PeriodStart,
                PeriodEnd: PeriodStart.AddMonths(1),
                CurrentValue: 2m,
                LimitValue: 10m),
            Now,
            CancellationToken.None);

        await repository.SaveChangesAsync(CancellationToken.None);

        var saved = await dbContext.TenantUsageCounters.SingleAsync();

        Assert.Equal(counter.Id, saved.Id);
        Assert.Equal(tenantId, saved.TenantId);
        Assert.Equal(limitId, saved.FeatureLimitDefinitionId);
        Assert.Equal(featureId, saved.PlatformFeatureId);
        Assert.Equal(TenantUsageCounterAlignmentConstants.UsageScope.Tenant, saved.UsageScope);
        Assert.Equal(2m, saved.CurrentValue);
        Assert.Equal(2m, saved.UsedQuantity);
        Assert.Equal(10m, saved.LimitValue);
        Assert.Equal(PeriodStart, saved.PeriodStart);
        Assert.Equal(PeriodStart.ToString("O"), saved.UsagePeriodStart);
        Assert.Equal(SubscriptionCatalogConstants.RecordStatus.Active, saved.Status);
    }

    [Fact]
    public async Task UpsertAsync_UpdatesExistingCounterInsteadOfInsertingDuplicate()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var limitId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId);
        SeedFeatureAndLimit(dbContext, featureId, limitId, "user_accounts", "max_users");
        await dbContext.SaveChangesAsync();

        ITenantUsageCounterRepository repository = new TenantUsageCounterRepository(dbContext);
        var request = new TenantUsageCounterUpsertRequest(
            tenantId,
            limitId,
            TenantUsageCounterAlignmentConstants.UsageScope.Tenant,
            null,
            PeriodStart,
            CurrentValue: 1m,
            LimitValue: 5m);

        var created = await repository.UpsertAsync(request, Now, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var updated = await repository.UpsertAsync(
            request with { CurrentValue = 3m, LimitValue = 8m },
            Now.AddMinutes(1),
            CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(1, await dbContext.TenantUsageCounters.CountAsync());
        Assert.Equal(3m, updated.CurrentValue);
        Assert.Equal(3m, updated.UsedQuantity);
        Assert.Equal(8m, updated.LimitValue);
    }

    [Fact]
    public async Task IncrementCurrentValueAsync_UpdatesCurrentValueAndUsedQuantity()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var limitId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId);
        SeedFeatureAndLimit(dbContext, featureId, limitId, "till_management", "max_tills");
        await dbContext.SaveChangesAsync();

        ITenantUsageCounterRepository repository = new TenantUsageCounterRepository(dbContext);
        var key = new TenantUsageCounterKey(
            tenantId,
            limitId,
            TenantUsageCounterAlignmentConstants.UsageScope.Tenant,
            null,
            PeriodStart);

        await repository.UpsertAsync(
            new TenantUsageCounterUpsertRequest(
                tenantId,
                limitId,
                TenantUsageCounterAlignmentConstants.UsageScope.Tenant,
                null,
                PeriodStart,
                CurrentValue: 2m),
            Now,
            CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var incremented = await repository.IncrementCurrentValueAsync(key, 1.5m, Now.AddMinutes(2), CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(3.5m, incremented.CurrentValue);
        Assert.Equal(3.5m, incremented.UsedQuantity);
    }

    [Fact]
    public async Task GetByKeyAsync_ReturnsMatchingCounter()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var limitId = Guid.NewGuid();
        var scopeReferenceId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId);
        SeedFeatureAndLimit(dbContext, featureId, limitId, "outlet_management", "max_outlets");
        await dbContext.SaveChangesAsync();

        ITenantUsageCounterRepository repository = new TenantUsageCounterRepository(dbContext);
        var key = new TenantUsageCounterKey(
            tenantId,
            limitId,
            TenantUsageCounterAlignmentConstants.UsageScope.Outlet,
            scopeReferenceId,
            PeriodStart);

        await repository.UpsertAsync(
            new TenantUsageCounterUpsertRequest(
                tenantId,
                limitId,
                TenantUsageCounterAlignmentConstants.UsageScope.Outlet,
                scopeReferenceId,
                PeriodStart,
                CurrentValue: 4m),
            Now,
            CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var found = await repository.GetByKeyAsync(key, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(4m, found!.CurrentValue);
        Assert.Equal(scopeReferenceId, found.ScopeReferenceId);
    }

    [Fact]
    public async Task UpsertAsync_ThrowsWhenFeatureLimitDefinitionIsInvalid()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId);
        await dbContext.SaveChangesAsync();

        ITenantUsageCounterRepository repository = new TenantUsageCounterRepository(dbContext);
        var missingLimitId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<InactiveFeatureLimitDefinitionException>(() =>
            repository.UpsertAsync(
                new TenantUsageCounterUpsertRequest(
                    tenantId,
                    missingLimitId,
                    TenantUsageCounterAlignmentConstants.UsageScope.Tenant,
                    null,
                    PeriodStart),
                Now,
                CancellationToken.None));

        Assert.Equal(missingLimitId, exception.FeatureLimitDefinitionId);
    }

    private static void SeedTenant(EPosDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            "TEN-USG",
            "ten-usg",
            "Usage Tenant",
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
}
