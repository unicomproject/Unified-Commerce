using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Options;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Services;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace E_POS.IntegrationTests.OutletTillDevice;

public sealed class TenantAdminTillCrudIntegrationTests
{
    [Fact]
    public async Task CreateAsync_WithHardwareFields_PersistsTill()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outlet = CreateOutlet(tenantId, "MAIN", "ACTIVE");
        dbContext.Outlets.Add(outlet);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, Now);

        var result = await service.CreateAsync(
            CreateContext(tenantId),
            CreateRequest(
                outlet.Id,
                tillCode: "HW-01",
                deviceName: "Counter Tablet",
                printerName: "Receipt Printer",
                scannerName: "Barcode Scanner",
                cashDrawerName: "Drawer A",
                cardReaderName: "Card Reader",
                internalNote: "Test hardware note"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("HW-01", result.Value!.TillCode);
        Assert.Equal("Counter Tablet", result.Value.DeviceName);
        Assert.Equal("Receipt Printer", result.Value.PrinterName);
        Assert.Equal("Barcode Scanner", result.Value.ScannerName);
        Assert.Equal("Drawer A", result.Value.CashDrawerName);
        Assert.Equal("Card Reader", result.Value.CardReaderName);
        Assert.Equal("Test hardware note", result.Value.InternalNote);
        Assert.Equal(1, await dbContext.Tills.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WithoutHardwareFields_PersistsTill()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outlet = CreateOutlet(tenantId, "MAIN", "ACTIVE");
        dbContext.Outlets.Add(outlet);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, Now);

        var result = await service.CreateAsync(
            CreateContext(tenantId),
            CreateRequest(outlet.Id, tillCode: "BASIC-01"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("BASIC-01", result.Value!.TillCode);
        Assert.Null(result.Value.DeviceName);
        Assert.Null(result.Value.PrinterName);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateTillCode_ReturnsDuplicateCode()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outlet = CreateOutlet(tenantId, "MAIN", "ACTIVE");
        dbContext.Outlets.Add(outlet);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, Now);
        await service.CreateAsync(
            CreateContext(tenantId),
            CreateRequest(outlet.Id, tillCode: "DUP-01"),
            CancellationToken.None);

        var duplicate = await service.CreateAsync(
            CreateContext(tenantId),
            CreateRequest(outlet.Id, tillCode: "DUP-01"),
            CancellationToken.None);

        Assert.True(duplicate.IsFailure);
        Assert.Equal("till.duplicate_code", duplicate.Error.Code);
    }

    private static readonly DateTimeOffset Now = new(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new EPosDbContext(options);
    }

    private static TenantAdminTillService CreateService(EPosDbContext dbContext, DateTimeOffset now)
    {
        var options = new FakeTillMonitoringOptionsSnapshot(new TillMonitoringOptions { HeartbeatTimeoutSeconds = 300 });
        var dateTimeProvider = new FakeDateTimeProvider(now);
        return new TenantAdminTillService(
            new TenantAdminTillRepository(dbContext, options, dateTimeProvider),
            new E_POS.Infrastructure.Modules.Tenant.HardwareCash.Repositories.TenantAdminHardwareRepository(dbContext),
            new TillDeviceAssignmentRepository(dbContext),
            dateTimeProvider,
            options,
            new AllowingTenantResourceLimitGuard());
    }

    private sealed class FakeTillMonitoringOptionsSnapshot : Microsoft.Extensions.Options.IOptionsSnapshot<TillMonitoringOptions>
    {
        public FakeTillMonitoringOptionsSnapshot(TillMonitoringOptions value)
        {
            Value = value;
        }

        public TillMonitoringOptions Value { get; }
        public TillMonitoringOptions Get(string? name) => Value;
    }

    private static TenantRequestContext CreateContext(Guid tenantId)
    {
        return new TenantRequestContext(
            tenantId,
            Guid.NewGuid(),
            [TenantAdminTillPermissions.Create, TenantAdminTillPermissions.Manage]);
    }

    private static TenantAdminTillCreateRequest CreateRequest(
        Guid outletId,
        string tillCode = "TILL-01",
        string? deviceName = null,
        string? printerName = null,
        string? scannerName = null,
        string? cashDrawerName = null,
        string? cardReaderName = null,
        string? internalNote = null)
    {
        return new TenantAdminTillCreateRequest(
            "Main Till",
            tillCode,
            outletId,
            TillConstants.ActiveStatus,
            0m,
            null,
            null,
            null,
            deviceName,
            printerName,
            scannerName,
            cashDrawerName,
            cardReaderName,
            internalNote);
    }

    private static Outlet CreateOutlet(Guid tenantId, string outletCode, string status)
    {
        return Outlet.Create(
            Guid.NewGuid(),
            tenantId,
            "Outlet " + outletCode,
            outletCode,
            status,
            "STORE",
            "UTC",
            false,
            null,
            null,
            null,
            Now);
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
