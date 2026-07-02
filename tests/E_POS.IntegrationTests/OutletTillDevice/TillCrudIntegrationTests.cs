using System.Reflection;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.OutletTillDevice.Dtos;
using E_POS.Application.Modules.OutletTillDevice.Services;
using E_POS.Application.Modules.OutletTillDevice.Validators;
using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Constants;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Infrastructure.Modules.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.OutletTillDevice;

public sealed class TillCrudIntegrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_PersistsTillForTenantOutlet()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outlet = Outlet.Create(Guid.NewGuid(), tenantId, "Main Outlet", "MAIN", "ACTIVE", "STORE", true, null, null, Now);
        dbContext.Outlets.Add(outlet);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.CreateAsync(CreateContext(tenantId, [TillConstants.CreatePermission]), CreateRequest(outlet.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("TILL001", result.Value!.TillCode);
        Assert.Equal(1, await dbContext.Tills.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_WithDifferentTenant_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var outlet = Outlet.Create(Guid.NewGuid(), tenantId, "Main Outlet", "MAIN", "ACTIVE", "STORE", true, null, null, Now);
        var till = Till.Create(Guid.NewGuid(), tenantId, outlet.Id, "Till 001", "TILL001", "ACTIVE", Now);
        dbContext.Outlets.Add(outlet);
        dbContext.Tills.Add(till);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetByIdAsync(CreateContext(otherTenantId, [TillConstants.ViewPermission]), till.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.not_found", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_SecondTillWithoutCode_GeneratesNextTillCode()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outlet = Outlet.Create(Guid.NewGuid(), tenantId, "Main Outlet", "MAIN", "ACTIVE", "STORE", true, null, null, Now);
        dbContext.Outlets.Add(outlet);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        await service.CreateAsync(CreateContext(tenantId, [TillConstants.CreatePermission]), CreateRequest(outlet.Id), CancellationToken.None);

        var second = await service.CreateAsync(CreateContext(tenantId, [TillConstants.CreatePermission]), CreateRequest(outlet.Id), CancellationToken.None);

        Assert.True(second.IsSuccess);
        Assert.Equal("TILL002", second.Value!.TillCode);
        Assert.Equal(2, await dbContext.Tills.CountAsync());
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyCurrentTenantTills()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var outlet = Outlet.Create(Guid.NewGuid(), tenantId, "Main Outlet", "MAIN", "ACTIVE", "STORE", true, null, null, Now);
        var otherOutlet = Outlet.Create(Guid.NewGuid(), otherTenantId, "Other Outlet", "OTHER", "ACTIVE", "STORE", true, null, null, Now);
        dbContext.Outlets.AddRange(outlet, otherOutlet);
        dbContext.Tills.Add(Till.Create(Guid.NewGuid(), tenantId, outlet.Id, "Till 001", "TILL001", "ACTIVE", Now));
        dbContext.Tills.Add(Till.Create(Guid.NewGuid(), otherTenantId, otherOutlet.Id, "Other Till", "TILL002", "ACTIVE", Now));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.ListAsync(CreateContext(tenantId, [TillConstants.ViewPermission]), null, 1, 50, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("TILL001", result.Value.Items[0].TillCode);
    }

    [Fact]
    public async Task UpdateAsync_WithAssignedTillOutletChange_ReturnsConflict()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outlet = Outlet.Create(Guid.NewGuid(), tenantId, "Main Outlet", "MAIN", "ACTIVE", "STORE", true, null, null, Now);
        var otherOutlet = Outlet.Create(Guid.NewGuid(), tenantId, "Other Outlet", "OTHER", "ACTIVE", "STORE", true, null, null, Now);
        var till = Till.Create(Guid.NewGuid(), tenantId, outlet.Id, "Till 001", "TILL001", "ACTIVE", Now);
        dbContext.Outlets.AddRange(outlet, otherOutlet);
        dbContext.Tills.Add(till);
        dbContext.TillDeviceAssignments.Add(CreateTillDeviceAssignment(till.Id));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.UpdateAsync(CreateContext(tenantId, [TillConstants.UpdatePermission]), till.Id, new TillUpdateRequest(otherOutlet.Id, "Till 001", "ACTIVE"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("till.outlet_change_conflict", result.Error.Code);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }

    private static TillService CreateService(EPosDbContext dbContext)
    {
        return new TillService(new TillRepository(dbContext), new CodeSequenceRepository(dbContext), new TillRequestValidator(), new FakeDateTimeProvider());
    }

    private static TenantRequestContext CreateContext(Guid tenantId, IReadOnlyCollection<string> permissions)
    {
        return new TenantRequestContext(tenantId, Guid.NewGuid(), permissions);
    }

    private static TillCreateRequest CreateRequest(Guid outletId)
    {
        return new TillCreateRequest(outletId, "Till 001", "ACTIVE");
    }

    private static TillDeviceAssignment CreateTillDeviceAssignment(Guid tillId)
    {
        var assignment = new TillDeviceAssignment();
        SetProperty(assignment, nameof(BaseEntity.Id), Guid.NewGuid());
        SetProperty(assignment, nameof(TillDeviceAssignment.TillId), tillId);
        SetProperty(assignment, nameof(TillDeviceAssignment.EffectiveFrom), "2026-07-02T10:00:00Z");
        SetProperty(assignment, nameof(AuditableEntity.CreatedAt), Now);
        SetProperty(assignment, nameof(AuditableEntity.UpdatedAt), Now);
        return assignment;
    }

    private static void SetProperty<T>(T target, string propertyName, object? value)
    {
        var type = target?.GetType() ?? throw new ArgumentNullException(nameof(target));
        while (type is not null)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property is not null)
            {
                property.SetValue(target, value);
                return;
            }

            type = type.BaseType;
        }

        throw new InvalidOperationException($"Property '{propertyName}' was not found.");
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}


