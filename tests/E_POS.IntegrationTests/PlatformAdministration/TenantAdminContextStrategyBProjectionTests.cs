using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.Subscription.Services;
using E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace E_POS.IntegrationTests.PlatformAdministration;

public sealed class TenantAdminContextStrategyBProjectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EnabledCanonical_MissingLegacy_ProjectsCanonicalOnly()
    {
        var result = await ProjectAsync(canonicalStatus: TenantEntitlementStatusConstants.Enabled, legacyStatus: null);

        Assert.Contains(PlatformTenantFeatureCodes.OutletManagement, result);
        Assert.DoesNotContain(PlatformTenantFeatureCodes.OutletManagementLegacyAlias, result);
        Assert.Equal(1, result.Count(x => PlatformTenantFeatureCodes.IsOutletManagementFeatureCode(x)));
    }

    [Fact]
    public async Task MissingCanonical_EnabledLegacy_ProjectsCanonicalOnly()
    {
        var result = await ProjectAsync(canonicalStatus: null, legacyStatus: TenantEntitlementStatusConstants.Enabled);

        Assert.Contains(PlatformTenantFeatureCodes.OutletManagement, result);
        Assert.DoesNotContain(PlatformTenantFeatureCodes.OutletManagementLegacyAlias, result);
    }

    [Fact]
    public async Task DisabledCanonical_EnabledLegacy_DoesNotProjectOutlet()
    {
        var result = await ProjectAsync(
            canonicalStatus: TenantEntitlementStatusConstants.Disabled,
            legacyStatus: TenantEntitlementStatusConstants.Enabled,
            canonicalIsEnabled: false);

        Assert.DoesNotContain(PlatformTenantFeatureCodes.OutletManagement, result);
        Assert.DoesNotContain(PlatformTenantFeatureCodes.OutletManagementLegacyAlias, result);
    }

    [Fact]
    public async Task ExpiredCanonical_EnabledLegacy_DoesNotProjectOutlet()
    {
        await using var db = CreateDbContext();
        var (tenantId, tenantUserId) = await SeedTenantUserAsync(db);
        var canonicalFeatureId = Guid.NewGuid();
        var legacyFeatureId = Guid.NewGuid();
        SeedFeature(db, canonicalFeatureId, PlatformTenantFeatureCodes.OutletManagement);
        SeedFeature(db, legacyFeatureId, PlatformTenantFeatureCodes.OutletManagementLegacyAlias);
        db.TenantFeatureEntitlements.Add(TenantFeatureEntitlement.Create(
            Guid.NewGuid(),
            tenantId,
            canonicalFeatureId,
            TenantEntitlementStatusConstants.Enabled,
            TenantEntitlementSourceTypeConstants.Manual,
            null,
            true,
            Now.AddDays(-10),
            Now.AddDays(-1),
            null,
            null,
            Now.AddDays(-10)));
        SeedEntitlement(db, tenantId, legacyFeatureId, TenantEntitlementStatusConstants.Enabled);
        await db.SaveChangesAsync();

        var result = await CreateRepository(db).GetContextDataAsync(tenantUserId, tenantId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.DoesNotContain(PlatformTenantFeatureCodes.OutletManagement, result!.EnabledFeatures);
        Assert.DoesNotContain(PlatformTenantFeatureCodes.OutletManagementLegacyAlias, result.EnabledFeatures);
    }

    [Fact]
    public async Task EnabledCanonical_DisabledLegacy_ProjectsCanonicalOnly()
    {
        var result = await ProjectAsync(
            canonicalStatus: TenantEntitlementStatusConstants.Enabled,
            legacyStatus: TenantEntitlementStatusConstants.Disabled,
            legacyIsEnabled: false);

        Assert.Contains(PlatformTenantFeatureCodes.OutletManagement, result);
        Assert.DoesNotContain(PlatformTenantFeatureCodes.OutletManagementLegacyAlias, result);
        Assert.Equal(1, result.Count(x => PlatformTenantFeatureCodes.IsOutletManagementFeatureCode(x)));
    }

    [Fact]
    public async Task BothEnabled_ProjectsSingleCanonical()
    {
        var result = await ProjectAsync(
            canonicalStatus: TenantEntitlementStatusConstants.Enabled,
            legacyStatus: TenantEntitlementStatusConstants.Enabled);

        Assert.Contains(PlatformTenantFeatureCodes.OutletManagement, result);
        Assert.DoesNotContain(PlatformTenantFeatureCodes.OutletManagementLegacyAlias, result);
        Assert.Equal(1, result.Count(x => x == PlatformTenantFeatureCodes.OutletManagement));
    }

    [Fact]
    public async Task MissingCanonical_DisabledLegacy_DoesNotProjectOutlet()
    {
        var result = await ProjectAsync(
            canonicalStatus: null,
            legacyStatus: TenantEntitlementStatusConstants.Disabled,
            legacyIsEnabled: false);

        Assert.DoesNotContain(PlatformTenantFeatureCodes.OutletManagement, result);
        Assert.DoesNotContain(PlatformTenantFeatureCodes.OutletManagementLegacyAlias, result);
    }

    [Fact]
    public async Task UnknownAliasEnabled_DoesNotProjectAsOutletManagement()
    {
        await using var db = CreateDbContext();
        var (tenantId, tenantUserId) = await SeedTenantUserAsync(db);
        var unknownFeatureId = Guid.NewGuid();
        SeedFeature(db, unknownFeatureId, "tenant.outlet");
        SeedEntitlement(db, tenantId, unknownFeatureId, TenantEntitlementStatusConstants.Enabled);
        await db.SaveChangesAsync();

        var result = await CreateRepository(db).GetContextDataAsync(tenantUserId, tenantId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("tenant.outlet", result!.EnabledFeatures);
        Assert.DoesNotContain(PlatformTenantFeatureCodes.OutletManagement, result.EnabledFeatures);
    }

    [Fact]
    public async Task Projection_DoesNotLeakOutletEntitlementAcrossTenants()
    {
        await using var db = CreateDbContext();
        var (tenantA, userA) = await SeedTenantUserAsync(db, "A");
        var (tenantB, userB) = await SeedTenantUserAsync(db, "B");
        var featureId = Guid.NewGuid();
        SeedFeature(db, featureId, PlatformTenantFeatureCodes.OutletManagement);
        SeedEntitlement(db, tenantA, featureId, TenantEntitlementStatusConstants.Enabled);
        await db.SaveChangesAsync();

        var resultB = await CreateRepository(db).GetContextDataAsync(userB, tenantB, CancellationToken.None);

        Assert.NotNull(resultB);
        Assert.DoesNotContain(PlatformTenantFeatureCodes.OutletManagement, resultB!.EnabledFeatures);
    }

    private static async Task<IReadOnlyList<string>> ProjectAsync(
        string? canonicalStatus,
        string? legacyStatus,
        bool canonicalIsEnabled = true,
        bool legacyIsEnabled = true)
    {
        await using var db = CreateDbContext();
        var (tenantId, tenantUserId) = await SeedTenantUserAsync(db);
        if (canonicalStatus is not null)
        {
            var canonicalFeatureId = Guid.NewGuid();
            SeedFeature(db, canonicalFeatureId, PlatformTenantFeatureCodes.OutletManagement);
            SeedEntitlement(db, tenantId, canonicalFeatureId, canonicalStatus, canonicalIsEnabled);
        }

        if (legacyStatus is not null)
        {
            var legacyFeatureId = Guid.NewGuid();
            SeedFeature(db, legacyFeatureId, PlatformTenantFeatureCodes.OutletManagementLegacyAlias);
            SeedEntitlement(db, tenantId, legacyFeatureId, legacyStatus, legacyIsEnabled);
        }

        await db.SaveChangesAsync();
        var result = await CreateRepository(db).GetContextDataAsync(tenantUserId, tenantId, CancellationToken.None);
        Assert.NotNull(result);
        return result!.EnabledFeatures;
    }

    private static TenantAdminContextRepository CreateRepository(EPosDbContext db) =>
        new(
            db,
            new TenantFeatureEntitlementEvaluator(db, NullLogger<TenantFeatureEntitlementEvaluator>.Instance));

    private static async Task<(Guid TenantId, Guid TenantUserId)> SeedTenantUserAsync(
        EPosDbContext db,
        string suffix = "1")
    {
        var tenantId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        db.Tenants.Add(Tenant.Create(
            tenantId,
            $"TEN-{suffix}-{tenantId:N}"[..12],
            $"ten-{suffix}-{tenantId:N}"[..20],
            $"Tenant {suffix}",
            "active",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            Now));
        db.TenantUsers.Add(TenantUser.Create(
            tenantUserId,
            tenantId,
            $"user{suffix}@test.local",
            $"User {suffix}",
            null,
            null,
            "hash",
            "salt",
            "ACTIVE",
            "admin",
            "admin",
            "HQ",
            Now));
        await Task.CompletedTask;
        return (tenantId, tenantUserId);
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
