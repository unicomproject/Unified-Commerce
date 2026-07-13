using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Services;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Validators;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Persistence;
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
        Assert.Equal(1, await dbContext.Outlets.CountAsync());
        Assert.Equal(1, await dbContext.OutletAddresses.CountAsync());
        Assert.Equal(2, await dbContext.OutletBusinessHours.CountAsync());
        Assert.Equal(1, await dbContext.FulfillmentMethodOutlets.CountAsync());
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

    private static async Task SeedTenantAsync(EPosDbContext dbContext, Guid tenantId)
    {
        var now = DateTimeOffset.UtcNow;
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
        await dbContext.SaveChangesAsync();
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

    private static TenantRequestContext CreateContext(Guid tenantId)
    {
        return new TenantRequestContext(tenantId, Guid.NewGuid(), [OutletConstants.ManagePermission]);
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
            collectionEnabled);
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
