using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Services;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Validators;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.OutletTillDevice;

public sealed class TillCrudIntegrationTests
{
    [Fact]
    public async Task CreateAsync_PersistsTillUnderActiveOutlet()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outlet = CreateOutlet(tenantId, "MAIN", "ACTIVE");
        dbContext.Outlets.Add(outlet);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, Now);

        var result = await service.CreateAsync(CreateContext(tenantId), CreateRequest(outlet.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("MAIN-01", result.Value!.TillCode);
        Assert.Equal(outlet.Id, result.Value.OutletId);
        Assert.Equal(1, await dbContext.Tills.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_WithDifferentTenant_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var outlet = CreateOutlet(tenantId, "MAIN", "ACTIVE");
        dbContext.Outlets.Add(outlet);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, Now);
        var create = await service.CreateAsync(CreateContext(tenantId), CreateRequest(outlet.Id), CancellationToken.None);

        var result = await service.GetByIdAsync(CreateContext(otherTenantId), create.Value!.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.not_found", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateTillCodeInSameOutlet_ReturnsDuplicateCode()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outlet = CreateOutlet(tenantId, "MAIN", "ACTIVE");
        dbContext.Outlets.Add(outlet);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, Now);
        await service.CreateAsync(CreateContext(tenantId), CreateRequest(outlet.Id), CancellationToken.None);

        var duplicate = await service.CreateAsync(CreateContext(tenantId), CreateRequest(outlet.Id), CancellationToken.None);

        Assert.True(duplicate.IsFailure);
        Assert.Equal("till.duplicate_code", duplicate.Error.Code);
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyCurrentTenantTills()
    {
        await using var dbContext = CreateDbContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var outletA = CreateOutlet(tenantA, "A", "ACTIVE");
        var outletB = CreateOutlet(tenantB, "B", "ACTIVE");
        dbContext.Outlets.AddRange(outletA, outletB);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, Now);
        await service.CreateAsync(CreateContext(tenantA), CreateRequest(outletA.Id), CancellationToken.None);
        await service.CreateAsync(CreateContext(tenantB), CreateRequest(outletB.Id), CancellationToken.None);

        var result = await service.ListAsync(CreateContext(tenantA), null, 1, 50, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(outletA.Id, result.Value.Items[0].OutletId);
    }

    private static readonly DateTimeOffset Now = new(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }

    private static TillService CreateService(EPosDbContext dbContext, DateTimeOffset now)
    {
        return new TillService(new TillRepository(dbContext), new TillRequestValidator(), new FakeDateTimeProvider(now));
    }

    private static TenantRequestContext CreateContext(Guid tenantId)
    {
        return new TenantRequestContext(tenantId, Guid.NewGuid(), [TillConstants.ManagePermission]);
    }

    private static TillCreateRequest CreateRequest(Guid outletId)
    {
        return new TillCreateRequest(outletId, "Main Till", "main-01", TillConstants.StandardTillType, 0m, TillConstants.DefaultCurrencyCode, true, "ACTIVE");
    }

    private static Outlet CreateOutlet(Guid tenantId, string outletCode, string status)
    {
        return Outlet.Create(Guid.NewGuid(), tenantId, "Outlet " + outletCode, outletCode, status, "STORE", "UTC", false, null, null, null, Now);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTimeOffset now)
        {
            UtcNow = now;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
