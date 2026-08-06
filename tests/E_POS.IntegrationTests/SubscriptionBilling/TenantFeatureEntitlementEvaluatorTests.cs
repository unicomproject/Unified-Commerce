using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.Subscription.Services;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace E_POS.IntegrationTests.SubscriptionBilling;

public sealed class TenantFeatureEntitlementEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Evaluate_MissingEntitlement_FailsClosed()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        SeedTenant(db, tenantId);
        SeedFeature(db, featureId, PlatformTenantFeatureCodes.OutletManagement);
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db);
        var result = await evaluator.EvaluateAsync(tenantId, PlatformTenantFeatureCodes.OutletManagement, Now, CancellationToken.None);

        Assert.Equal(TenantFeatureEntitlementDecision.Missing, result.Decision);
        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_EnabledCanonical_Allows()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        SeedTenant(db, tenantId);
        SeedFeature(db, featureId, PlatformTenantFeatureCodes.OutletManagement);
        SeedEntitlement(db, tenantId, featureId, TenantEntitlementStatusConstants.Enabled);
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db);
        var result = await evaluator.EvaluateAsync(tenantId, PlatformTenantFeatureCodes.OutletManagement, Now, CancellationToken.None);

        Assert.Equal(TenantFeatureEntitlementDecision.Allowed, result.Decision);
        Assert.False(result.UsedLegacyAlias);
    }

    [Fact]
    public async Task Evaluate_LegacyOnly_AllowsWithLegacyFlag()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var legacyFeatureId = Guid.NewGuid();
        SeedTenant(db, tenantId);
        SeedFeature(db, legacyFeatureId, PlatformTenantFeatureCodes.OutletManagementLegacyAlias);
        SeedEntitlement(db, tenantId, legacyFeatureId, TenantEntitlementStatusConstants.Enabled);
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db);
        var result = await evaluator.EvaluateAsync(
            tenantId,
            PlatformTenantFeatureCodes.OutletManagement,
            Now,
            CancellationToken.None);

        Assert.Equal(TenantFeatureEntitlementDecision.Allowed, result.Decision);
        Assert.True(result.UsedLegacyAlias);
        Assert.Equal(PlatformTenantFeatureCodes.OutletManagement, result.CanonicalFeatureCode);
    }

    [Fact]
    public async Task Evaluate_DisabledCanonical_WinsOverEnabledLegacy()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var canonicalFeatureId = Guid.NewGuid();
        var legacyFeatureId = Guid.NewGuid();
        SeedTenant(db, tenantId);
        SeedFeature(db, canonicalFeatureId, PlatformTenantFeatureCodes.OutletManagement);
        SeedFeature(db, legacyFeatureId, PlatformTenantFeatureCodes.OutletManagementLegacyAlias);
        SeedEntitlement(db, tenantId, canonicalFeatureId, TenantEntitlementStatusConstants.Disabled, isEnabled: false);
        SeedEntitlement(db, tenantId, legacyFeatureId, TenantEntitlementStatusConstants.Enabled);
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db);
        var result = await evaluator.EvaluateAsync(
            tenantId,
            PlatformTenantFeatureCodes.OutletManagementLegacyAlias,
            Now,
            CancellationToken.None);

        Assert.Equal(TenantFeatureEntitlementDecision.Disabled, result.Decision);
        Assert.False(result.IsAllowed);
        Assert.False(result.UsedLegacyAlias);
        Assert.True(result.FoundCanonicalRecord);
        Assert.True(result.FoundLegacyRecord);
    }

    [Fact]
    public async Task Evaluate_UnknownFeature_FailsClosed()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        SeedTenant(db, tenantId);
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db);
        var result = await evaluator.EvaluateAsync(tenantId, "totally_unknown_feature", Now, CancellationToken.None);

        Assert.Equal(TenantFeatureEntitlementDecision.UnknownFeature, result.Decision);
        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_ExpiredEntitlement_FailsClosed()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        SeedTenant(db, tenantId);
        SeedFeature(db, featureId, PlatformTenantFeatureCodes.OutletManagement);
        db.TenantFeatureEntitlements.Add(TenantFeatureEntitlement.Create(
            Guid.NewGuid(),
            tenantId,
            featureId,
            TenantEntitlementStatusConstants.Enabled,
            TenantEntitlementSourceTypeConstants.Manual,
            null,
            true,
            Now.AddDays(-10),
            Now.AddDays(-1),
            null,
            null,
            Now.AddDays(-10)));
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db);
        var result = await evaluator.EvaluateAsync(tenantId, PlatformTenantFeatureCodes.OutletManagement, Now, CancellationToken.None);

        Assert.Equal(TenantFeatureEntitlementDecision.Expired, result.Decision);
        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task Evaluate_DoesNotLeakAcrossTenants()
    {
        await using var db = CreateDbContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        SeedTenant(db, tenantA);
        SeedTenant(db, tenantB);
        SeedFeature(db, featureId, PlatformTenantFeatureCodes.OutletManagement);
        SeedEntitlement(db, tenantA, featureId, TenantEntitlementStatusConstants.Enabled);
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db);
        var result = await evaluator.EvaluateAsync(tenantB, PlatformTenantFeatureCodes.OutletManagement, Now, CancellationToken.None);

        Assert.Equal(TenantFeatureEntitlementDecision.Missing, result.Decision);
        Assert.False(result.IsAllowed);
    }

    private static TenantFeatureEntitlementEvaluator CreateEvaluator(EPosDbContext db) =>
        new(db, NullLogger<TenantFeatureEntitlementEvaluator>.Instance);

    private static void SeedTenant(EPosDbContext db, Guid tenantId)
    {
        db.Tenants.Add(Tenant.Create(
            tenantId,
            $"T-{tenantId:N}"[..12],
            $"tenant-{tenantId:N}"[..20],
            $"Tenant {tenantId:N}"[..20],
            "active",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));
    }

    private static void SeedFeature(EPosDbContext db, Guid featureId, string featureCode)
    {
        var moduleId = Guid.NewGuid();
        db.PlatformModules.Add(PlatformModule.Create(
            moduleId,
            featureCode,
            featureCode,
            description: null,
            status: SubscriptionCatalogConstants.RecordStatus.Active,
            sortOrder: 0,
            now: Now));
        db.PlatformFeatures.Add(PlatformFeature.Create(
            featureId,
            moduleId,
            featureCode,
            featureCode,
            SubscriptionCatalogConstants.RecordStatus.Active,
            Now));
    }

    private static void SeedEntitlement(
        EPosDbContext db,
        Guid tenantId,
        Guid featureId,
        string status,
        bool isEnabled = true)
    {
        db.TenantFeatureEntitlements.Add(TenantFeatureEntitlement.Create(
            Guid.NewGuid(),
            tenantId,
            featureId,
            status,
            TenantEntitlementSourceTypeConstants.Manual,
            null,
            isEnabled,
            Now.AddDays(-1),
            null,
            null,
            null,
            Now.AddDays(-1)));
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}
