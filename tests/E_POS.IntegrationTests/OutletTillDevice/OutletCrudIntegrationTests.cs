using System.Reflection;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Services;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Validators;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.OutletTillDevice;

public sealed class OutletCrudIntegrationTests
{
    [Fact]
    public async Task CreateAsync_PersistsOutletAddressHoursAndPickupMapping()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId);
        var now = new DateTimeOffset(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);
        dbContext.FulfillmentMethods.Add(FulfillmentMethod.Create(Guid.NewGuid(), tenantId, "PICKUP", "Pickup", null, "ACTIVE", "PICKUP", now));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, now);

        var result = await service.CreateAsync(CreateContext(tenantId), CreateRequest(collectionEnabled: true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("OUT001", result.Value!.OutletCode);
        Assert.True(result.Value.CollectionEnabled);

        Assert.Equal(30, result.Value.PreparationLeadMinutes);
        Assert.Equal(30, result.Value.PickupWindowMinutes);
        Assert.Equal(new TimeOnly(16, 0), result.Value.CollectionCutoffTime);

        Assert.Equal("Main Outlet", result.Value.OutletName);
        Assert.Equal("STORE", result.Value.OutletType);
        Assert.Equal("+94770000000", result.Value.Phone);
        Assert.Equal("main@example.com", result.Value.Email);
        Assert.Equal("UTC", result.Value.Timezone);
        Assert.Equal("ACTIVE", result.Value.Status);
        Assert.Equal(now, result.Value.CreatedAt);
        Assert.NotNull(result.Value.CreatedByTenantUserId);
        Assert.Equal(result.Value.CreatedByTenantUserId, result.Value.UpdatedByTenantUserId);

        Assert.Equal(1, await dbContext.Outlets.CountAsync());
        Assert.Equal(1, await dbContext.OutletAddresses.CountAsync());
        Assert.Equal(2, await dbContext.OutletBusinessHours.CountAsync());
        Assert.Equal(1, await dbContext.FulfillmentMethodOutlets.CountAsync());
        var mapping = await dbContext.FulfillmentMethodOutlets.SingleAsync();
        Assert.Equal(tenantId, mapping.TenantId);
        Assert.Equal(30, mapping.PreparationLeadMinutes);
        Assert.Equal(30, mapping.PickupWindowMinutes);
        Assert.Equal(new TimeOnly(16, 0), mapping.CutoffTime);

        var list = await service.ListAsync(CreateContext(tenantId), 1, 20, null, CancellationToken.None);
        var summary = Assert.Single(list.Value!.Items);
        Assert.True(summary.CollectionEnabled);
        Assert.Equal(30, summary.PreparationLeadMinutes);
        Assert.Equal(30, summary.PickupWindowMinutes);
        Assert.Equal(new TimeOnly(16, 0), summary.CollectionCutoffTime);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCollectionConfigurationOnTenantPickupMapping()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId);
        var now = new DateTimeOffset(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);
        dbContext.FulfillmentMethods.Add(FulfillmentMethod.Create(Guid.NewGuid(), tenantId, "PICKUP", "Pickup", null, "ACTIVE", "PICKUP", now));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, now);
        var create = await service.CreateAsync(CreateContext(tenantId), CreateRequest(collectionEnabled: true), CancellationToken.None);
        var outletId = create.Value!.Id;
        dbContext.ChangeTracker.Clear();
        service = CreateService(dbContext, now);
        var source = CreateRequest(collectionEnabled: true);
        var updateRequest = new OutletUpdateRequest(
            source.OutletName,
            source.Status,
            source.OutletType,
            source.Timezone,
            source.IsDefaultOutlet,
            source.Phone,
            source.Email,
            source.Address,
            source.BusinessHours,
            true,
            90,
            60,
            new TimeOnly(15, 30));

        var result = await service.UpdateAsync(CreateContext(tenantId), outletId, updateRequest, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(90, result.Value!.PreparationLeadMinutes);
        Assert.Equal(60, result.Value.PickupWindowMinutes);
        Assert.Equal(new TimeOnly(15, 30), result.Value.CollectionCutoffTime);
        var mapping = await dbContext.FulfillmentMethodOutlets.SingleAsync(x => x.TenantId == tenantId);
        Assert.Equal(90, mapping.PreparationLeadMinutes);
        Assert.Equal(60, mapping.PickupWindowMinutes);
        Assert.Equal(new TimeOnly(15, 30), mapping.CutoffTime);
    }

    [Fact]
    public async Task CreateAsync_WithNullPhoneAndEmail_PersistsNullValues()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId);
        var service = CreateService(dbContext, DateTimeOffset.UtcNow);

        var result = await service.CreateAsync(
            CreateContext(tenantId),
            CreateRequest(collectionEnabled: false) with { Phone = " ", Email = " " },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Phone);
        Assert.Null(result.Value.Email);
        var outlet = await dbContext.Outlets.SingleAsync(x => x.Id == result.Value.Id);
        Assert.Null(outlet.Phone);
        Assert.Null(outlet.Email);
    }

    [Fact]
    public async Task CreateAsync_WithInactiveStatus_PersistsOperationalStatus()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId);
        var service = CreateService(dbContext, DateTimeOffset.UtcNow);

        var result = await service.CreateAsync(
            CreateContext(tenantId),
            CreateRequest(collectionEnabled: false) with { Status = OutletConstants.InactiveStatus },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(OutletConstants.InactiveStatus, result.Value!.Status);
        Assert.Equal(OutletConstants.InactiveStatus, (await dbContext.Outlets.SingleAsync(x => x.Id == result.Value.Id)).Status);
    }

    [Fact]
    public async Task GetByIdAsync_WithDifferentTenant_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId);
        var otherTenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, otherTenantId);
        var service = CreateService(dbContext, DateTimeOffset.UtcNow);
        var create = await service.CreateAsync(CreateContext(tenantId), CreateRequest(collectionEnabled: false), CancellationToken.None);

        var result = await service.GetByIdAsync(CreateContext(otherTenantId), create.Value!.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("outlet.not_found", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_SecondOutletWithoutCode_GeneratesNextOutletCode()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId);
        var service = CreateService(dbContext, DateTimeOffset.UtcNow);
        await service.CreateAsync(CreateContext(tenantId), CreateRequest(collectionEnabled: false), CancellationToken.None);

        var second = await service.CreateAsync(CreateContext(tenantId), CreateRequest(collectionEnabled: false), CancellationToken.None);

        Assert.True(second.IsSuccess);
        Assert.Equal("OUT002", second.Value!.OutletCode);
        Assert.Equal(2, await dbContext.Outlets.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WithDefaultOutlet_ClearsPreviousDefault()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId);
        var service = CreateService(dbContext, DateTimeOffset.UtcNow);
        var first = await service.CreateAsync(
            CreateContext(tenantId),
            CreateRequest(collectionEnabled: false) with { IsDefaultOutlet = true },
            CancellationToken.None);
        var second = await service.CreateAsync(
            CreateContext(tenantId),
            CreateRequest(collectionEnabled: false) with { OutletName = "Second Outlet", IsDefaultOutlet = true },
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        var outlets = await dbContext.Outlets.Where(x => x.TenantId == tenantId).ToListAsync();
        Assert.Equal(1, outlets.Count(x => x.IsDefaultOutlet));
        Assert.False(outlets.Single(x => x.Id == first.Value!.Id).IsDefaultOutlet);
        Assert.True(outlets.Single(x => x.Id == second.Value!.Id).IsDefaultOutlet);
    }

    [Fact]
    public async Task UpdateAsync_WhenOutletBecomesDefault_ReplacesDefaultOnlyInSameTenant()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId);
        await SeedTenantAsync(dbContext, otherTenantId);
        var service = CreateService(dbContext, DateTimeOffset.UtcNow);
        var first = await service.CreateAsync(
            CreateContext(tenantId),
            CreateRequest(collectionEnabled: false) with { IsDefaultOutlet = true },
            CancellationToken.None);
        var second = await service.CreateAsync(
            CreateContext(tenantId),
            CreateRequest(collectionEnabled: false) with { OutletName = "Second Outlet" },
            CancellationToken.None);
        var otherTenantDefault = await service.CreateAsync(
            CreateContext(otherTenantId),
            CreateRequest(collectionEnabled: false) with { IsDefaultOutlet = true },
            CancellationToken.None);
        var updateUserId = Guid.NewGuid();
        var updateTime = DateTimeOffset.UtcNow.AddMinutes(5);
        service = CreateService(dbContext, updateTime);

        var update = await service.UpdateAsync(
            CreateContext(tenantId, updateUserId),
            second.Value!.Id,
            CreateUpdateRequest() with { OutletName = "Second Outlet", IsDefaultOutlet = true },
            CancellationToken.None);

        Assert.True(update.IsSuccess);
        var tenantOutlets = await dbContext.Outlets.Where(x => x.TenantId == tenantId).ToListAsync();
        Assert.False(tenantOutlets.Single(x => x.Id == first.Value!.Id).IsDefaultOutlet);
        Assert.True(tenantOutlets.Single(x => x.Id == second.Value.Id).IsDefaultOutlet);
        Assert.True((await dbContext.Outlets.SingleAsync(x => x.Id == otherTenantDefault.Value!.Id)).IsDefaultOutlet);
        Assert.Equal(updateUserId, tenantOutlets.Single(x => x.Id == second.Value.Id).UpdatedByTenantUserId);
        Assert.Equal(updateTime, tenantOutlets.Single(x => x.Id == second.Value.Id).UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_PreservesCodeAndCreatedAudit_AndUpdatesUpdatedAudit()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId);
        var createdBy = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var service = CreateService(dbContext, createdAt);
        var create = await service.CreateAsync(CreateContext(tenantId, createdBy), CreateRequest(collectionEnabled: false), CancellationToken.None);
        var updatedBy = Guid.NewGuid();
        var updatedAt = createdAt.AddMinutes(10);
        service = CreateService(dbContext, updatedAt);

        var update = await service.UpdateAsync(
            CreateContext(tenantId, updatedBy),
            create.Value!.Id,
            CreateUpdateRequest() with { OutletName = "Updated Outlet", Phone = null, Email = null },
            CancellationToken.None);

        Assert.True(update.IsSuccess);
        Assert.Equal(create.Value.OutletCode, update.Value!.OutletCode);
        Assert.Equal(createdAt, update.Value.CreatedAt);
        Assert.Equal(createdBy, update.Value.CreatedByTenantUserId);
        Assert.Equal(updatedAt, update.Value.UpdatedAt);
        Assert.Equal(updatedBy, update.Value.UpdatedByTenantUserId);
        Assert.Null(update.Value.Phone);
        Assert.Null(update.Value.Email);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesAndClearsDefaultOutletAudit()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId);
        var createdAt = DateTimeOffset.UtcNow;
        var service = CreateService(dbContext, createdAt);
        var create = await service.CreateAsync(
            CreateContext(tenantId),
            CreateRequest(collectionEnabled: false) with { IsDefaultOutlet = true },
            CancellationToken.None);
        var deletedBy = Guid.NewGuid();
        var deletedAt = createdAt.AddMinutes(15);
        service = CreateService(dbContext, deletedAt);

        var delete = await service.DeleteAsync(CreateContext(tenantId, deletedBy), create.Value!.Id, CancellationToken.None);

        Assert.True(delete.IsSuccess);
        var outlet = await dbContext.Outlets.SingleAsync(x => x.Id == create.Value.Id);
        Assert.Equal(OutletConstants.DeletedStatus, outlet.Status);
        Assert.False(outlet.IsDefaultOutlet);
        Assert.Equal(deletedAt, outlet.UpdatedAt);
        Assert.Equal(deletedBy, outlet.UpdatedByTenantUserId);
    }

    [Fact]
    public async Task GetCreateOptionsAsync_ReturnsSupportedLookups()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId);
        var service = CreateService(dbContext, DateTimeOffset.UtcNow);

        var result = await service.GetCreateOptionsAsync(CreateContext(tenantId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.OutletTypes, item => item.Value == "STORE");
        Assert.Contains(result.Value.Countries, item => item.Code == "LK");
        Assert.Contains(result.Value.Timezones, item => item.Value == "UTC");
    }

    [Fact]
    public async Task GetActivePickupFulfillmentMethodIdAsync_PrefersDefaultThenStableOrdering()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId);
        var now = new DateTimeOffset(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);
        var alphabeticalMethod = FulfillmentMethod.Create(
            Guid.NewGuid(),
            tenantId,
            "AAA_PICKUP",
            "Alphabetical Pickup",
            null,
            "ACTIVE",
            "PICKUP",
            now);
        var defaultMethod = FulfillmentMethod.Create(
            Guid.NewGuid(),
            tenantId,
            "ZZZ_PICKUP",
            "Default Pickup",
            null,
            "ACTIVE",
            "PICKUP",
            now);
        Set(defaultMethod, "IsDefault", true);
        dbContext.FulfillmentMethods.AddRange(alphabeticalMethod, defaultMethod);
        await dbContext.SaveChangesAsync();
        var repository = new OutletRepository(dbContext);

        var selectedMethodId = await repository.GetActivePickupFulfillmentMethodIdAsync(
            tenantId,
            CancellationToken.None);

        Assert.Equal(defaultMethod.Id, selectedMethodId);
    }

    private static async Task SeedTenantAsync(EPosDbContext dbContext, Guid tenantId)
    {
        var now = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            $"TEN-{tenantId.ToString()[..8]}",
            $"tenant-{tenantId.ToString()[..8]}",
            "Test Tenant",
            TenantStatusConstants.Active,
            "LKR",
            "UTC",
            null,
            null,
            now));
        if (!await dbContext.PlatformFeatures.AnyAsync(
                x => x.Id == SubscriptionBillingCatalogSeedConstants.ClickCollectFeatureId))
        {
            dbContext.PlatformFeatures.Add(PlatformFeature.Create(
                SubscriptionBillingCatalogSeedConstants.ClickCollectFeatureId,
                SubscriptionBillingCatalogSeedConstants.CoreCommerceModuleId,
                PlatformTenantFeatureCodes.ClickCollect,
                "Click & Collect",
                SubscriptionCatalogConstants.RecordStatus.Active,
                now));
        }
        dbContext.TenantFeatureEntitlements.Add(TenantFeatureEntitlement.Create(
            Guid.NewGuid(),
            tenantId,
            SubscriptionBillingCatalogSeedConstants.ClickCollectFeatureId,
            TenantEntitlementStatusConstants.Enabled,
            now));
        await dbContext.SaveChangesAsync();
    }

    private static void Set<T>(object entity, string propertyName, T value)
    {
        var property = entity.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        property?.SetValue(entity, value);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }

    private static OutletService CreateService(EPosDbContext dbContext, DateTimeOffset now)
    {
        return new OutletService(
            new OutletRepository(dbContext),
            new CodeSequenceRepository(dbContext),
            new OutletRequestValidator(),
            new FakeOutletAuditLogger(),
            new FakeDateTimeProvider(now));
    }

    private static TenantRequestContext CreateContext(Guid tenantId, Guid? userId = null)
    {
        return new TenantRequestContext(tenantId, userId ?? Guid.NewGuid(), [OutletConstants.ManagePermission]);
    }

    private static OutletCreateRequest CreateRequest(bool collectionEnabled)
    {
        return new OutletCreateRequest(
            "Main Outlet",
            "ACTIVE",
            "STORE",
            "UTC",
            false,
            "+94770000000",
            "main@example.com",
            new OutletAddressRequest("1 Main Street", "Level 1", "Colombo", "Western", "00100", "LK", null, null),
            [
                new OutletBusinessHourRequest(1, new TimeOnly(9, 0), new TimeOnly(17, 0), false, null, null),
                new OutletBusinessHourRequest(2, new TimeOnly(9, 0), new TimeOnly(17, 0), false, null, null)
            ],
            collectionEnabled,
            collectionEnabled ? 30 : null,
            collectionEnabled ? 30 : null,
            collectionEnabled ? new TimeOnly(16, 0) : null);
    }

    private static OutletUpdateRequest CreateUpdateRequest()
    {
        return new OutletUpdateRequest(
            "Updated Outlet",
            "ACTIVE",
            "STORE",
            "UTC",
            false,
            "+94770000001",
            "updated@example.com",
            new OutletAddressRequest("2 Main Street", "Level 2", "Colombo", "Western", "00100", "LK", null, null),
            [
                new OutletBusinessHourRequest(1, new TimeOnly(8, 0), new TimeOnly(18, 0), false, null, null),
                new OutletBusinessHourRequest(2, new TimeOnly(8, 0), new TimeOnly(18, 0), false, null, null)
            ],
            false);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTimeOffset now)
        {
            UtcNow = now;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FakeOutletAuditLogger : IOutletAuditLogger
    {
        public int CreatedCount { get; private set; }

        public void LogOutletCreated(Guid tenantId, Guid actorTenantUserId, Guid outletId, string outletCode, string outletType, string status)
        {
            CreatedCount++;
        }
    }
}
