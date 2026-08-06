using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Services;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Validators;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.Subscription.Services;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace E_POS.IntegrationTests.SubscriptionBilling;

public sealed class TenantResourceLimitEnforcementTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OutletCreate_WithinLimit_Succeeds_ThenDeniesAtLimit()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedTenantWithOutletLimitAsync(db, tenantId, planLimit: 3, overrideLimit: 2);

        var service = CreateOutletService(db);
        var first = await service.CreateAsync(CreateOutletContext(tenantId), CreateOutletRequest("A"), CancellationToken.None);
        var second = await service.CreateAsync(CreateOutletContext(tenantId), CreateOutletRequest("B"), CancellationToken.None);
        var third = await service.CreateAsync(CreateOutletContext(tenantId), CreateOutletRequest("C"), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.True(third.IsFailure);
        Assert.Equal(SubscriptionLimitErrorCodes.LimitReached, third.Error.Code);
        Assert.Equal(2, await db.Outlets.CountAsync(x => x.TenantId == tenantId && x.Status != OutletConstants.DeletedStatus));
    }

    [Fact]
    public async Task OutletCreate_UsesTenantOverrideAbovePlan()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedTenantWithOutletLimitAsync(db, tenantId, planLimit: 1, overrideLimit: 2);

        var service = CreateOutletService(db);
        Assert.True((await service.CreateAsync(CreateOutletContext(tenantId), CreateOutletRequest("A"), CancellationToken.None)).IsSuccess);
        Assert.True((await service.CreateAsync(CreateOutletContext(tenantId), CreateOutletRequest("B"), CancellationToken.None)).IsSuccess);
        var denied = await service.CreateAsync(CreateOutletContext(tenantId), CreateOutletRequest("C"), CancellationToken.None);

        Assert.True(denied.IsFailure);
        Assert.Equal(SubscriptionLimitErrorCodes.LimitReached, denied.Error.Code);
    }

    [Fact]
    public async Task OutletCreate_InactiveOutletStillCountsTowardLimit()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedTenantWithOutletLimitAsync(db, tenantId, planLimit: 1, overrideLimit: 1);

        var service = CreateOutletService(db);
        var inactiveRequest = CreateOutletRequest("Inactive") with { Status = OutletConstants.InactiveStatus };
        Assert.True((await service.CreateAsync(CreateOutletContext(tenantId), inactiveRequest, CancellationToken.None)).IsSuccess);

        var denied = await service.CreateAsync(CreateOutletContext(tenantId), CreateOutletRequest("Second"), CancellationToken.None);
        Assert.True(denied.IsFailure);
        Assert.Equal(SubscriptionLimitErrorCodes.LimitReached, denied.Error.Code);
    }

    [Fact]
    public async Task OutletCreate_TenantIsolation_DoesNotCountOtherTenant()
    {
        await using var db = CreateDb();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await SeedTenantWithOutletLimitAsync(db, tenantA, planLimit: 1, overrideLimit: 1);
        await SeedTenantWithOutletLimitAsync(db, tenantB, planLimit: 1, overrideLimit: 1);

        var service = CreateOutletService(db);
        Assert.True((await service.CreateAsync(CreateOutletContext(tenantA), CreateOutletRequest("A1"), CancellationToken.None)).IsSuccess);
        Assert.True((await service.CreateAsync(CreateOutletContext(tenantB), CreateOutletRequest("B1"), CancellationToken.None)).IsSuccess);

        var deniedA = await service.CreateAsync(CreateOutletContext(tenantA), CreateOutletRequest("A2"), CancellationToken.None);
        Assert.True(deniedA.IsFailure);
        Assert.Equal(SubscriptionLimitErrorCodes.LimitReached, deniedA.Error.Code);
    }

    [Fact]
    public async Task OutletCreate_MissingSubscription_FailsSafely()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedTenantEntitlementOnlyAsync(db, tenantId);

        var service = CreateOutletService(db);
        var result = await service.CreateAsync(CreateOutletContext(tenantId), CreateOutletRequest("A"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(SubscriptionLimitErrorCodes.ConfigurationMissing, result.Error.Code);
        Assert.Equal(0, await db.Outlets.CountAsync(x => x.TenantId == tenantId));
    }

    [Fact]
    public async Task Resolver_UnknownLimitKey_Fails()
    {
        await using var db = CreateDb();
        var resolver = new TenantSubscriptionLimitResolver(
            db,
            new FixedDateTimeProvider(Now),
            NullLogger<TenantSubscriptionLimitResolver>.Instance);

        var result = await resolver.ResolveAsync(Guid.NewGuid(), "max_widgets", CancellationToken.None);

        Assert.False(result.IsConfigurationValid);
        Assert.Equal(SubscriptionLimitErrorCodes.UnknownKey, result.FailureCode);
    }

    [Fact]
    public async Task Resolver_NullOverride_FallsBackToFinitePlan()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedTenantWithOutletLimitAsync(db, tenantId, planLimit: 3, overrideLimit: null);

        var resolver = new TenantSubscriptionLimitResolver(
            db,
            new FixedDateTimeProvider(Now),
            NullLogger<TenantSubscriptionLimitResolver>.Instance);
        var result = await resolver.ResolveAsync(tenantId, TenantSubscriptionLimitKeys.MaxOutlets, CancellationToken.None);

        Assert.True(result.IsConfigurationValid);
        Assert.False(result.IsUnlimited);
        Assert.False(result.OverrideApplied);
        Assert.Equal(3, result.PlanLimit);
        Assert.Equal(3, result.EffectiveLimit);
        Assert.Null(result.OverrideLimit);
    }

    [Fact]
    public async Task Resolver_NullOverride_WithUnlimitedPlan_IsUnlimited()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedTenantWithOutletLimitAsync(db, tenantId, planLimit: null, overrideLimit: null);

        var resolver = new TenantSubscriptionLimitResolver(
            db,
            new FixedDateTimeProvider(Now),
            NullLogger<TenantSubscriptionLimitResolver>.Instance);
        var result = await resolver.ResolveAsync(tenantId, TenantSubscriptionLimitKeys.MaxOutlets, CancellationToken.None);

        Assert.True(result.IsConfigurationValid);
        Assert.True(result.IsUnlimited);
        Assert.Null(result.EffectiveLimit);
        Assert.False(result.OverrideApplied);
    }

    [Fact]
    public async Task OutletCreate_NullOverride_AtPlanLimit_IsDenied()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedTenantWithOutletLimitAsync(db, tenantId, planLimit: 2, overrideLimit: null);

        var service = CreateOutletService(db);
        Assert.True((await service.CreateAsync(CreateOutletContext(tenantId), CreateOutletRequest("A"), CancellationToken.None)).IsSuccess);
        Assert.True((await service.CreateAsync(CreateOutletContext(tenantId), CreateOutletRequest("B"), CancellationToken.None)).IsSuccess);

        var denied = await service.CreateAsync(CreateOutletContext(tenantId), CreateOutletRequest("C"), CancellationToken.None);
        Assert.True(denied.IsFailure);
        Assert.Equal(SubscriptionLimitErrorCodes.LimitReached, denied.Error.Code);
        Assert.Equal(2, await db.Outlets.CountAsync(x => x.TenantId == tenantId && x.Status != OutletConstants.DeletedStatus));
    }

    [Fact]
    public async Task GetCreateOptions_NullOverride_ReportsPlanCapacity()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedTenantWithOutletLimitAsync(db, tenantId, planLimit: 3, overrideLimit: null);

        var service = CreateOutletService(db);
        Assert.True((await service.CreateAsync(CreateOutletContext(tenantId), CreateOutletRequest("A"), CancellationToken.None)).IsSuccess);
        Assert.True((await service.CreateAsync(CreateOutletContext(tenantId), CreateOutletRequest("B"), CancellationToken.None)).IsSuccess);

        var options = await service.GetCreateOptionsAsync(CreateOutletContext(tenantId), CancellationToken.None);
        Assert.True(options.IsSuccess);
        Assert.NotNull(options.Value!.Capacity);
        Assert.Equal(2, options.Value.Capacity!.CurrentUsage);
        Assert.Equal(3, options.Value.Capacity.EffectiveLimit);
        Assert.Equal(1, options.Value.Capacity.RemainingCapacity);
        Assert.True(options.Value.Capacity.CanCreate);
        Assert.False(options.Value.Capacity.IsUnlimited);
        Assert.False(options.Value.Capacity.OverrideApplied);
    }

    [Fact]
    public async Task Resolver_InvalidNegativeOverride_FailsSafely()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedTenantWithOutletLimitAsync(db, tenantId, planLimit: 3, overrideLimit: -1);

        var resolver = new TenantSubscriptionLimitResolver(
            db,
            new FixedDateTimeProvider(Now),
            NullLogger<TenantSubscriptionLimitResolver>.Instance);
        var result = await resolver.ResolveAsync(tenantId, TenantSubscriptionLimitKeys.MaxOutlets, CancellationToken.None);

        Assert.False(result.IsConfigurationValid);
        Assert.Equal(SubscriptionLimitErrorCodes.Invalid, result.FailureCode);
        Assert.False(result.IsUnlimited);
    }

    [Fact]
    public async Task Resolver_ZeroOverride_IsExplicitBlock()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedTenantWithOutletLimitAsync(db, tenantId, planLimit: 3, overrideLimit: 0);

        var resolver = new TenantSubscriptionLimitResolver(
            db,
            new FixedDateTimeProvider(Now),
            NullLogger<TenantSubscriptionLimitResolver>.Instance);
        var result = await resolver.ResolveAsync(tenantId, TenantSubscriptionLimitKeys.MaxOutlets, CancellationToken.None);

        Assert.True(result.IsConfigurationValid);
        Assert.True(result.OverrideApplied);
        Assert.Equal(0, result.EffectiveLimit);
        Assert.False(result.IsUnlimited);
    }

    [Fact]
    public async Task Resolver_ActiveAddon_IncreasesPlanBaselineWhenNoOverride()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var subscriptionId = await SeedTenantWithOutletLimitReturningSubscriptionIdAsync(
            db,
            tenantId,
            planLimit: 3,
            overrideLimit: null);

        var addonId = Guid.NewGuid();
        db.SubscriptionAddons.Add(SubscriptionAddon.Create(
            addonId,
            SubscriptionCatalogLimitSeedConstants.ExtraOutletAddonCode,
            "Extra Outlet",
            SubscriptionCatalogConstants.RecordStatus.Active,
            100m,
            Now));
        db.SubscriptionAddonLimits.Add(SubscriptionAddonLimit.Create(
            Guid.NewGuid(),
            addonId,
            SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId,
            2m,
            Now));
        db.TenantSubscriptionAddons.Add(TenantSubscriptionAddon.Create(
            Guid.NewGuid(),
            subscriptionId,
            addonId,
            quantity: 1,
            status: SubscriptionCatalogConstants.RecordStatus.Active,
            unitPrice: 100m,
            currencyCode: "LKR",
            autoRenew: true,
            startsAt: Now.AddDays(-1),
            endsAt: null,
            createdByPlatformUserId: null,
            updatedByPlatformUserId: null,
            now: Now));
        await db.SaveChangesAsync();

        var resolver = new TenantSubscriptionLimitResolver(
            db,
            new FixedDateTimeProvider(Now),
            NullLogger<TenantSubscriptionLimitResolver>.Instance);
        var result = await resolver.ResolveAsync(tenantId, TenantSubscriptionLimitKeys.MaxOutlets, CancellationToken.None);

        Assert.True(result.IsConfigurationValid);
        Assert.Equal(5, result.PlanLimit);
        Assert.Equal(5, result.EffectiveLimit);
        Assert.False(result.OverrideApplied);
    }

    [Fact]
    public async Task Resolver_ExpiredAddon_IsIgnored()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var subscriptionId = await SeedTenantWithOutletLimitReturningSubscriptionIdAsync(
            db,
            tenantId,
            planLimit: 3,
            overrideLimit: null);

        var addonId = Guid.NewGuid();
        db.SubscriptionAddons.Add(SubscriptionAddon.Create(
            addonId,
            "expired_outlet",
            "Expired Outlet",
            SubscriptionCatalogConstants.RecordStatus.Active,
            100m,
            Now));
        db.SubscriptionAddonLimits.Add(SubscriptionAddonLimit.Create(
            Guid.NewGuid(),
            addonId,
            SubscriptionCatalogLimitSeedConstants.MaxOutletsLimitDefinitionId,
            10m,
            Now));
        db.TenantSubscriptionAddons.Add(TenantSubscriptionAddon.Create(
            Guid.NewGuid(),
            subscriptionId,
            addonId,
            quantity: 1,
            status: SubscriptionCatalogConstants.RecordStatus.Active,
            unitPrice: 100m,
            currencyCode: "LKR",
            autoRenew: true,
            startsAt: Now.AddDays(-30),
            endsAt: Now.AddDays(-1),
            createdByPlatformUserId: null,
            updatedByPlatformUserId: null,
            now: Now));
        await db.SaveChangesAsync();

        var resolver = new TenantSubscriptionLimitResolver(
            db,
            new FixedDateTimeProvider(Now),
            NullLogger<TenantSubscriptionLimitResolver>.Instance);
        var result = await resolver.ResolveAsync(tenantId, TenantSubscriptionLimitKeys.MaxOutlets, CancellationToken.None);

        Assert.True(result.IsConfigurationValid);
        Assert.Equal(3, result.EffectiveLimit);
    }

    [Fact]
    public async Task OutletCreate_LegacyNullOverride_AtPlanLimit_IsDenied()
    {
        // Legacy create writes null Max*Override; corrected resolver must enforce plan baseline.
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedTenantWithOutletLimitAsync(db, tenantId, planLimit: 1, overrideLimit: null);

        var service = CreateOutletService(db);
        Assert.True((await service.CreateAsync(CreateOutletContext(tenantId), CreateOutletRequest("Only"), CancellationToken.None)).IsSuccess);
        var denied = await service.CreateAsync(CreateOutletContext(tenantId), CreateOutletRequest("Extra"), CancellationToken.None);

        Assert.True(denied.IsFailure);
        Assert.Equal(SubscriptionLimitErrorCodes.LimitReached, denied.Error.Code);
        Assert.Equal(1, await db.Outlets.CountAsync(x => x.TenantId == tenantId && x.Status != OutletConstants.DeletedStatus));
    }

    private static OutletService CreateOutletService(EPosDbContext db) =>
        new(
            new OutletRepository(db),
            new CodeSequenceRepository(db),
            new OutletRequestValidator(),
            new FakeOutletAuditLogger(),
            new FixedDateTimeProvider(Now),
            new TenantFeatureEntitlementEvaluator(db, NullLogger<TenantFeatureEntitlementEvaluator>.Instance),
            new TenantResourceLimitGuard(
                db,
                new TenantSubscriptionLimitResolver(db, new FixedDateTimeProvider(Now), NullLogger<TenantSubscriptionLimitResolver>.Instance),
                new FixedDateTimeProvider(Now),
                NullLogger<TenantResourceLimitGuard>.Instance));

    private static async Task SeedTenantWithOutletLimitAsync(
        EPosDbContext db,
        Guid tenantId,
        int? planLimit,
        int? overrideLimit) =>
        await SeedTenantWithOutletLimitReturningSubscriptionIdAsync(db, tenantId, planLimit, overrideLimit);

    private static async Task<Guid> SeedTenantWithOutletLimitReturningSubscriptionIdAsync(
        EPosDbContext db,
        Guid tenantId,
        int? planLimit,
        int? overrideLimit)
    {
        await SeedTenantEntitlementOnlyAsync(db, tenantId);

        var planId = Guid.NewGuid();
        db.SubscriptionPlans.Add(SubscriptionPlan.Create(
            planId,
            $"plan-{planId.ToString()[..8]}",
            "Limited Plan",
            "active",
            "monthly",
            1000m,
            Now,
            maxOutlets: planLimit,
            maxUsers: 10,
            maxTills: 10));

        var subscriptionId = Guid.NewGuid();
        db.TenantSubscriptions.Add(TenantSubscription.Create(
            subscriptionId,
            tenantId,
            planId,
            TenantSubscriptionStatusConstants.Active,
            TenantSubscriptionBillingConstants.BillingCycleMonthly,
            trialStartAt: null,
            trialEndAt: null,
            billingStartAt: Now,
            nextBillingAt: null,
            autoRenew: true,
            discountType: null,
            discountValue: null,
            taxPercentage: 0m,
            invoiceEmail: null,
            paymentMethod: null,
            notes: null,
            maxOutletsOverride: overrideLimit,
            maxTillsOverride: 10,
            maxUsersOverride: 10,
            currencyCode: "LKR",
            planPrice: 1000m,
            startedAt: Now,
            currentPeriodStart: Now,
            currentPeriodEnd: null,
            assignedByPlatformUserId: null,
            Now));

        await db.SaveChangesAsync();
        return subscriptionId;
    }

    private static async Task SeedTenantEntitlementOnlyAsync(EPosDbContext db, Guid tenantId)
    {
        db.Tenants.Add(Tenant.Create(
            tenantId,
            $"TEN-{tenantId.ToString()[..8]}",
            $"tenant-{tenantId.ToString()[..8]}",
            "Limit Tenant",
            TenantStatusConstants.Active,
            "LKR",
            "UTC",
            null,
            null,
            Now));

        var outletFeatureId = Guid.Parse("72000000-0000-0000-0000-0000000000A1");
        if (!await db.PlatformFeatures.AnyAsync(x => x.Id == outletFeatureId))
        {
            db.PlatformFeatures.Add(PlatformFeature.Create(
                outletFeatureId,
                SubscriptionBillingCatalogSeedConstants.CoreCommerceModuleId,
                PlatformTenantFeatureCodes.OutletManagement,
                "Outlet Management",
                SubscriptionCatalogConstants.RecordStatus.Active,
                Now));
        }

        db.TenantFeatureEntitlements.Add(TenantFeatureEntitlement.Create(
            Guid.NewGuid(),
            tenantId,
            outletFeatureId,
            TenantEntitlementStatusConstants.Enabled,
            Now));

        await db.SaveChangesAsync();
    }

    private static TenantRequestContext CreateOutletContext(Guid tenantId) =>
        new(tenantId, Guid.NewGuid(), [OutletConstants.ManagePermission]);

    private static OutletCreateRequest CreateOutletRequest(string name) =>
        new(
            name,
            OutletConstants.ActiveStatus,
            "STORE",
            "UTC",
            false,
            null,
            null,
            new OutletAddressRequest("1 Street", null, "Colombo", "Western", "00100", "LK", null, null),
            [new OutletBusinessHourRequest(1, new TimeOnly(9, 0), new TimeOnly(17, 0), false, null, null)],
            false);

    private static EPosDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }

    private sealed class FixedDateTimeProvider(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class FakeOutletAuditLogger : IOutletAuditLogger
    {
        public void LogOutletCreated(Guid tenantId, Guid actorTenantUserId, Guid outletId, string outletCode, string outletType, string status) { }
        public void LogManagerAssigned(Guid tenantId, Guid actorTenantUserId, Guid outletId, Guid managerTenantUserId) { }
        public void LogManagerRemoved(Guid tenantId, Guid actorTenantUserId, Guid outletId) { }
        public void LogImageAssociated(Guid tenantId, Guid actorTenantUserId, Guid outletId, Guid mediaAssetId) { }
        public void LogImageRemoved(Guid tenantId, Guid actorTenantUserId, Guid outletId) { }
        public void LogStatusChanged(Guid tenantId, Guid actorTenantUserId, Guid outletId, string status) { }
    }
}
