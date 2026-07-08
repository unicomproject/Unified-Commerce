using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.POSOperations.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace E_POS.IntegrationTests.POSOperations;

public sealed class PosHomeDashboardRepositoryTests
{
    [Fact]
    public async Task ResolveContextAsync_WhenOutletIdMismatchesTill_StillResolvesContext()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var wrongOutletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 8, 10, 0, 0, TimeSpan.Zero);

        await SeedResolvedContextAsync(
            dbContext,
            tenantId,
            userId,
            outletId,
            tillId,
            deviceId,
            now);

        var repository = CreateRepository(dbContext);
        var context = new TenantRequestContext(tenantId, userId, ["pos.home.view"]);

        var resolution = await repository.ResolveContextAsync(
            context,
            wrongOutletId,
            tillId,
            deviceId,
            CancellationToken.None);

        Assert.True(resolution.IsResolved, resolution.ReasonCode ?? "unknown");
        Assert.NotNull(resolution.Snapshot);
        Assert.Equal("Front Till 01", resolution.Snapshot!.TillName);
        Assert.Equal("Front", resolution.Snapshot.TillAreaName);
        Assert.Equal(1, resolution.Snapshot.TillNumber);
        Assert.Equal("Main Outlet", resolution.Snapshot.OutletName);
    }

    [Fact]
    public async Task ResolveContextAsync_WhenNoOpenTillSession_ReturnsReasonCode()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 8, 10, 0, 0, TimeSpan.Zero);

        await SeedResolvedContextAsync(
            dbContext,
            tenantId,
            userId,
            outletId,
            tillId,
            deviceId,
            now,
            includeOpenSession: false);

        var repository = CreateRepository(dbContext);
        var context = new TenantRequestContext(tenantId, userId, ["pos.home.view"]);

        var resolution = await repository.ResolveContextAsync(
            context,
            outletId,
            tillId,
            deviceId,
            CancellationToken.None);

        Assert.False(resolution.IsResolved);
        Assert.Equal(PosHomeContextReasonCodes.NoOpenTillSession, resolution.ReasonCode);
    }

    [Fact]
    public async Task ResolveContextAsync_WhenDeviceIdProvided_ResolvesTillFromAssignment()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var wrongTillId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 8, 10, 0, 0, TimeSpan.Zero);

        await SeedResolvedContextAsync(
            dbContext,
            tenantId,
            userId,
            outletId,
            tillId,
            deviceId,
            now);

        var repository = CreateRepository(dbContext);
        var context = new TenantRequestContext(tenantId, userId, ["pos.home.view"]);

        var resolution = await repository.ResolveContextAsync(
            context,
            outletId,
            wrongTillId,
            deviceId,
            CancellationToken.None);

        Assert.True(resolution.IsResolved, resolution.ReasonCode ?? "unknown");
        Assert.Equal(tillId, resolution.Snapshot!.TillId);
    }

    private static async Task SeedResolvedContextAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid userId,
        Guid outletId,
        Guid tillId,
        Guid deviceId,
        DateTimeOffset now,
        bool includeOpenSession = true)
    {
        dbContext.Currencies.Add(Currency.Create(
            Guid.NewGuid(),
            "LKR",
            "Sri Lankan Rupee",
            "Rs",
            2,
            true,
            1,
            now));

        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            "DEV-001",
            "dev-001",
            "Test Tenant",
            "active",
            "LKR",
            "Asia/Colombo",
            null,
            null,
            now));

        dbContext.TenantUsers.Add(TenantUser.Create(
            userId,
            tenantId,
            "cashier@test.com",
            "Cashier 001",
            null,
            null,
            "hash",
            "salt",
            "ACTIVE",
            "cashier",
            "outlet",
            outletId.ToString(),
            now));

        dbContext.Outlets.Add(Outlet.Create(
            outletId,
            tenantId,
            "Main Outlet",
            "MAIN-01",
            "ACTIVE",
            "STORE",
            "UTC",
            true,
            null,
            null,
            null,
            now));

        dbContext.Tills.Add(Till.Create(
            tillId,
            tenantId,
            outletId,
            "Front Till 01",
            "Front",
            1,
            "FRONT-01",
            "STANDARD",
            0m,
            "LKR",
            true,
            "ACTIVE",
            null,
            now));

        dbContext.PosDevices.Add(PosDevice.Create(
            deviceId,
            tenantId,
            outletId,
            "POS-01",
            "Front POS Device",
            "TABLET",
            "ACTIVE",
            null,
            now));

        dbContext.TillDeviceAssignments.Add(
            TillDeviceAssignment.Create(Guid.NewGuid(), tenantId, outletId, tillId, deviceId, null, now));

        if (includeOpenSession)
        {
            dbContext.TillSessions.Add(TillSession.Open(
                Guid.NewGuid(),
                tenantId,
                outletId,
                tillId,
                "TS-0001",
                DateOnly.FromDateTime(now.UtcDateTime),
                userId,
                deviceId,
                0,
                "LKR",
                null,
                now));
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"POS home test seed failed: {ex.Message}", ex);
        }
    }

    private static PosHomeDashboardRepository CreateRepository(EPosDbContext dbContext) =>
        new(dbContext, NullLogger<PosHomeDashboardRepository>.Instance);

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
