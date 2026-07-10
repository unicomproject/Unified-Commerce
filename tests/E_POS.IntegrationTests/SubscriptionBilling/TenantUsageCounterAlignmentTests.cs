using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.SubscriptionBilling;

public sealed class TenantUsageCounterAlignmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 5, 10, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CreatedAt = new(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_DualWritesLegacyAndSecondBrainColumns()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var limitId = Guid.NewGuid();
        var periodStart = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
        var periodEnd = periodStart.AddMonths(1);

        SeedTenant(dbContext, tenantId);
        SeedFeatureAndLimit(dbContext, featureId, limitId, "outlet_management", "max_outlets");

        var counter = TenantUsageCounter.Create(
            Guid.NewGuid(),
            tenantId,
            limitId,
            featureId,
            TenantUsageCounterAlignmentConstants.UsageScope.Tenant,
            scopeReferenceId: null,
            currentValue: 3m,
            limitValue: 10m,
            periodStart,
            periodEnd,
            Now);

        dbContext.TenantUsageCounters.Add(counter);
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.TenantUsageCounters.SingleAsync();

        Assert.Equal(tenantId, saved.TenantId);
        Assert.Equal(featureId, saved.PlatformFeatureId);
        Assert.Equal(limitId, saved.FeatureLimitDefinitionId);
        Assert.Equal(TenantUsageCounterAlignmentConstants.UsageScope.Tenant, saved.UsageScope);
        Assert.Null(saved.ScopeReferenceId);
        Assert.Equal(3m, saved.UsedQuantity);
        Assert.Equal(3m, saved.CurrentValue);
        Assert.Equal(10m, saved.LimitValue);
        Assert.Equal(periodStart, saved.PeriodStart);
        Assert.Equal(periodEnd, saved.PeriodEnd);
        Assert.Equal(periodStart.ToString("O"), saved.UsagePeriodStart);
        Assert.Equal(SubscriptionCatalogConstants.RecordStatus.Active, saved.Status);
    }

    [Fact]
    public void SetCurrentValue_UpdatesCurrentValueAndUsedQuantity()
    {
        var counter = TenantUsageCounter.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TenantUsageCounterAlignmentConstants.UsageScope.Tenant,
            null,
            1m,
            5m,
            Now,
            null,
            Now);

        counter.SetCurrentValue(4.5m, Now.AddMinutes(1));

        Assert.Equal(4.5m, counter.CurrentValue);
        Assert.Equal(4.5m, counter.UsedQuantity);
        Assert.Equal(Now.AddMinutes(1), counter.UpdatedAt);
    }

    [Fact]
    public async Task Backfill_MapsFeatureLimitDefinitionId_WhenExactlyOneActiveLimitExists()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var limitId = Guid.NewGuid();

        SeedTenant(dbContext, tenantId);
        SeedFeatureAndLimit(dbContext, featureId, limitId, "user_accounts", "max_users");

        dbContext.TenantUsageCounters.Add(TenantUsageCounter.CreateLegacy(
            Guid.NewGuid(),
            tenantId,
            featureId,
            "2026-07-01T00:00:00.0000000Z",
            2m,
            CreatedAt));

        await dbContext.SaveChangesAsync();
        await TenantUsageCounterAlignmentBackfillApplicator.ApplyAsync(dbContext);

        var saved = await dbContext.TenantUsageCounters.SingleAsync();
        Assert.Equal(limitId, saved.FeatureLimitDefinitionId);
        Assert.Equal(2m, saved.CurrentValue);
        Assert.Equal(2m, saved.UsedQuantity);
        Assert.Equal(TenantUsageCounterAlignmentConstants.UsageScope.Tenant, saved.UsageScope);
        Assert.Equal(SubscriptionCatalogConstants.RecordStatus.Active, saved.Status);
    }

    [Fact]
    public async Task Backfill_Throws_WhenMultipleActiveLimitsExist()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();

        SeedTenant(dbContext, tenantId);
        SeedFeature(dbContext, featureId, "till_management");

        dbContext.FeatureLimitDefinitions.AddRange(
            FeatureLimitDefinition.Create(
                Guid.NewGuid(),
                featureId,
                "max_tills",
                "Maximum Tills",
                2m,
                CreatedAt),
            FeatureLimitDefinition.Create(
                Guid.NewGuid(),
                featureId,
                "max_tills_extra",
                "Maximum Tills Extra",
                1m,
                CreatedAt));

        dbContext.TenantUsageCounters.Add(TenantUsageCounter.CreateLegacy(
            Guid.NewGuid(),
            tenantId,
            featureId,
            "invalid-period",
            1m,
            CreatedAt));

        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<TenantUsageCounterAlignmentBackfillApplicator.AmbiguousFeatureLimitMappingException>(
            () => TenantUsageCounterAlignmentBackfillApplicator.ApplyAsync(dbContext));

        Assert.Equal(featureId, exception.PlatformFeatureId);
        Assert.Equal(2, exception.ActiveLimitCount);
    }

    [Fact]
    public async Task Backfill_Throws_WhenNoActiveLimitExists()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();

        SeedTenant(dbContext, tenantId);
        SeedFeature(dbContext, featureId, "orphan_feature");

        dbContext.TenantUsageCounters.Add(TenantUsageCounter.CreateLegacy(
            Guid.NewGuid(),
            tenantId,
            featureId,
            "invalid-period",
            1m,
            CreatedAt));

        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<TenantUsageCounterAlignmentBackfillApplicator.AmbiguousFeatureLimitMappingException>(
            () => TenantUsageCounterAlignmentBackfillApplicator.ApplyAsync(dbContext));

        Assert.Equal(featureId, exception.PlatformFeatureId);
        Assert.Equal(0, exception.ActiveLimitCount);
    }

    [Fact]
    public async Task Backfill_MirrorsUsedQuantityToCurrentValue()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var limitId = Guid.NewGuid();

        SeedTenant(dbContext, tenantId);
        SeedFeatureAndLimit(dbContext, featureId, limitId, "outlet_management", "max_outlets");

        dbContext.TenantUsageCounters.Add(TenantUsageCounter.CreateLegacy(
            Guid.NewGuid(),
            tenantId,
            featureId,
            "not-parseable",
            7.25m,
            CreatedAt));

        await dbContext.SaveChangesAsync();
        await TenantUsageCounterAlignmentBackfillApplicator.ApplyAsync(dbContext);

        var saved = await dbContext.TenantUsageCounters.SingleAsync();
        Assert.Equal(7.25m, saved.UsedQuantity);
        Assert.Equal(7.25m, saved.CurrentValue);
    }

    [Theory]
    [InlineData("2026-07-15T12:30:00Z", "2026-07-15T12:30:00Z")]
    [InlineData("2026-07-15", "2026-07-15T00:00:00Z")]
    public void ParseUsagePeriodStart_ParsesValidValues(string input, string expectedUtc)
    {
        var parsed = TenantUsageCounter.ParseUsagePeriodStart(input, CreatedAt);
        var expected = DateTimeOffset.Parse(expectedUtc, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(expected.UtcDateTime, parsed.UtcDateTime);
    }

    [Fact]
    public void ParseUsagePeriodStart_FallsBackToCreatedAtForInvalidValues()
    {
        var parsed = TenantUsageCounter.ParseUsagePeriodStart("not-a-real-period", CreatedAt);
        Assert.Equal(CreatedAt, parsed);
    }

    [Fact]
    public async Task Backfill_UsesCreatedAtWhenUsagePeriodStartIsInvalid()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var limitId = Guid.NewGuid();

        SeedTenant(dbContext, tenantId);
        SeedFeatureAndLimit(dbContext, featureId, limitId, "outlet_management", "max_outlets");

        dbContext.TenantUsageCounters.Add(TenantUsageCounter.CreateLegacy(
            Guid.NewGuid(),
            tenantId,
            featureId,
            "legacy-free-form",
            1m,
            CreatedAt));

        await dbContext.SaveChangesAsync();
        await TenantUsageCounterAlignmentBackfillApplicator.ApplyAsync(dbContext);

        var saved = await dbContext.TenantUsageCounters.SingleAsync();
        Assert.Equal(CreatedAt, saved.PeriodStart);
    }

    [Fact]
    public async Task PersistsAllSecondBrainColumns()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        var limitId = Guid.NewGuid();
        var scopeReferenceId = Guid.NewGuid();
        var periodStart = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
        var periodEnd = periodStart.AddMonths(1);
        var lastCalculatedAt = Now.AddMinutes(-5);

        SeedTenant(dbContext, tenantId);
        SeedFeatureAndLimit(dbContext, featureId, limitId, "outlet_management", "max_outlets");

        var counter = TenantUsageCounter.Create(
            Guid.NewGuid(),
            tenantId,
            limitId,
            featureId,
            TenantUsageCounterAlignmentConstants.UsageScope.Outlet,
            scopeReferenceId,
            2m,
            8m,
            periodStart,
            periodEnd,
            Now,
            lastCalculatedAt: lastCalculatedAt);

        dbContext.TenantUsageCounters.Add(counter);
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.TenantUsageCounters.AsNoTracking().SingleAsync();

        Assert.Equal(limitId, saved.FeatureLimitDefinitionId);
        Assert.Equal(TenantUsageCounterAlignmentConstants.UsageScope.Outlet, saved.UsageScope);
        Assert.Equal(scopeReferenceId, saved.ScopeReferenceId);
        Assert.Equal(2m, saved.CurrentValue);
        Assert.Equal(8m, saved.LimitValue);
        Assert.Equal(periodStart, saved.PeriodStart);
        Assert.Equal(periodEnd, saved.PeriodEnd);
        Assert.Equal(lastCalculatedAt, saved.LastCalculatedAt);
        Assert.Equal(SubscriptionCatalogConstants.RecordStatus.Active, saved.Status);
    }

    private static void SeedTenant(EPosDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            "TEN-USAGE",
            "ten-usage",
            "Usage Tenant",
            "active",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            CreatedAt));
    }

    private static void SeedFeature(EPosDbContext dbContext, Guid featureId, string featureCode)
    {
        var moduleId = Guid.NewGuid();
        dbContext.PlatformModules.Add(PlatformModule.Create(
            moduleId,
            featureCode,
            featureCode,
            description: null,
            status: "active",
            sortOrder: 0,
            now: CreatedAt));

        dbContext.PlatformFeatures.Add(PlatformFeature.Create(
            featureId,
            moduleId,
            featureCode,
            featureCode,
            "active",
            CreatedAt));
    }

    private static void SeedFeatureAndLimit(
        EPosDbContext dbContext,
        Guid featureId,
        Guid limitId,
        string featureCode,
        string limitCode)
    {
        SeedFeature(dbContext, featureId, featureCode);

        dbContext.FeatureLimitDefinitions.Add(FeatureLimitDefinition.Create(
            limitId,
            featureId,
            limitCode,
            limitCode,
            5m,
            CreatedAt));
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
