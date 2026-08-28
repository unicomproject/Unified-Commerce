using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using E_POS.Domain.Modules.Shared.ReturnExchange.Entities;
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
        const string deviceFingerprint = "pos-web-test-installation-01";

        await SeedResolvedContextAsync(
            dbContext,
            tenantId,
            userId,
            outletId,
            tillId,
            deviceId,
            now,
            deviceFingerprint);

        var repository = CreateRepository(dbContext);
        var context = new TenantRequestContext(tenantId, userId, ["pos.home.view"]);

        var resolution = await repository.ResolveContextAsync(
            context,
            wrongOutletId,
            tillId,
            deviceId,
            deviceFingerprint,
            CancellationToken.None);

        Assert.True(resolution.IsResolved, resolution.ReasonCode ?? "unknown");
        Assert.NotNull(resolution.Snapshot);
        Assert.Equal("Front Till 01", resolution.Snapshot!.TillName);
        Assert.Equal("Front", resolution.Snapshot.TillAreaName);
        Assert.Equal(1, resolution.Snapshot.TillNumber);
        Assert.Equal("Main Outlet", resolution.Snapshot.OutletName);
    }

    [Fact]
    public async Task ResolveContextAsync_WhenCurrentSessionHasSales_ReturnsLiveSessionSummary()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var salesChannelId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 8, 10, 0, 0, TimeSpan.Zero);
        const string deviceFingerprint = "pos-web-test-installation-summary";

        await SeedResolvedContextAsync(
            dbContext,
            tenantId,
            userId,
            outletId,
            tillId,
            deviceId,
            now,
            deviceFingerprint);

        var tillSessionId = await dbContext.TillSessions
            .Where(x => x.TenantId == tenantId && x.TillId == tillId)
            .Select(x => x.Id)
            .SingleAsync();
        var firstOrderId = Guid.NewGuid();
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            firstOrderId,
            tenantId,
            "SO-0001",
            salesChannelId,
            null,
            null,
            tillId,
            tillSessionId,
            null,
            "LKR",
            false,
            1000m,
            100m,
            0m,
            900m,
            900m,
            userId,
            now));
        dbContext.SalesOrders.Add(SalesOrder.CreateCompletedPosSale(
            Guid.NewGuid(),
            tenantId,
            "SO-0002",
            salesChannelId,
            null,
            null,
            tillId,
            tillSessionId,
            null,
            "LKR",
            false,
            500m,
            0m,
            0m,
            500m,
            500m,
            userId,
            now));
        dbContext.SalesReturns.Add(SalesReturn.CreateCompleted(
            Guid.NewGuid(),
            tenantId,
            firstOrderId,
            null,
            outletId,
            Guid.NewGuid(),
            "RET-0001",
            1m,
            200m,
            null,
            "return-summary-test",
            userId,
            now));
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var context = new TenantRequestContext(tenantId, userId, ["pos.home.view"]);

        var resolution = await repository.ResolveContextAsync(
            context,
            outletId,
            tillId,
            deviceId,
            deviceFingerprint,
            CancellationToken.None);

        Assert.True(resolution.IsResolved, resolution.ReasonCode ?? "unknown");
        Assert.NotNull(resolution.Snapshot);
        Assert.Equal(1500, resolution.Snapshot!.GrossSalesAmount);
        Assert.Equal(2, resolution.Snapshot.TransactionCount);
        Assert.Equal(100, resolution.Snapshot.DiscountAmount);
        Assert.Equal(200, resolution.Snapshot.RefundAmount);
        Assert.Equal(1, resolution.Snapshot.RefundCount);
        Assert.Equal(1200, resolution.Snapshot.NetSalesAmount);
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
        const string deviceFingerprint = "pos-web-test-installation-02";

        await SeedResolvedContextAsync(
            dbContext,
            tenantId,
            userId,
            outletId,
            tillId,
            deviceId,
            now,
            deviceFingerprint,
            includeOpenSession: false);

        var repository = CreateRepository(dbContext);
        var context = new TenantRequestContext(tenantId, userId, ["pos.home.view"]);

        var resolution = await repository.ResolveContextAsync(
            context,
            outletId,
            tillId,
            deviceId,
            deviceFingerprint,
            CancellationToken.None);

        Assert.False(resolution.IsResolved);
        Assert.Equal(PosHomeContextReasonCodes.NoOpenTillSession, resolution.ReasonCode);
    }

    [Fact]
    public async Task ResolveContextAsync_WhenTillHintDoesNotMatchDeviceAssignment_ReturnsReasonCode()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var wrongTillId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 8, 10, 0, 0, TimeSpan.Zero);
        const string deviceFingerprint = "pos-web-test-installation-03";

        await SeedResolvedContextAsync(
            dbContext,
            tenantId,
            userId,
            outletId,
            tillId,
            deviceId,
            now,
            deviceFingerprint);

        var repository = CreateRepository(dbContext);
        var context = new TenantRequestContext(tenantId, userId, ["pos.home.view"]);

        var resolution = await repository.ResolveContextAsync(
            context,
            outletId,
            wrongTillId,
            deviceId,
            deviceFingerprint,
            CancellationToken.None);

        Assert.False(resolution.IsResolved);
        Assert.Equal(PosHomeContextReasonCodes.DeviceNotAssignedToTill, resolution.ReasonCode);
    }

    [Fact]
    public async Task ResolveContextAsync_WhenDeviceFingerprintMissing_DoesNotUseLatestAssignment()
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
            "pos-web-test-installation-04");

        var repository = CreateRepository(dbContext);
        var context = new TenantRequestContext(tenantId, userId, ["pos.home.view"]);

        var resolution = await repository.ResolveContextAsync(
            context,
            outletId,
            tillId,
            null,
            null,
            CancellationToken.None);

        Assert.False(resolution.IsResolved);
        Assert.Equal(PosHomeContextReasonCodes.DeviceContextMissing, resolution.ReasonCode);
    }

    [Fact]
    public async Task ResolveContextAsync_WhenDeviceIsActiveButNotTrusted_ReturnsDeviceNotTrusted()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 8, 10, 0, 0, TimeSpan.Zero);
        const string deviceFingerprint = "pos-web-test-installation-05";

        await SeedResolvedContextAsync(
            dbContext,
            tenantId,
            userId,
            outletId,
            tillId,
            deviceId,
            now,
            deviceFingerprint,
            isTrusted: false);

        var repository = CreateRepository(dbContext);
        var context = new TenantRequestContext(tenantId, userId, ["pos.home.view"]);

        var resolution = await repository.ResolveContextAsync(
            context,
            outletId,
            tillId,
            deviceId,
            deviceFingerprint,
            CancellationToken.None);

        Assert.False(resolution.IsResolved);
        Assert.Equal(PosHomeContextReasonCodes.DeviceNotTrusted, resolution.ReasonCode);
    }

    private static async Task SeedResolvedContextAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid userId,
        Guid outletId,
        Guid tillId,
        Guid deviceId,
        DateTimeOffset now,
        string deviceFingerprint,
        bool isTrusted = true,
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
            now,
            staffCode: "USR-2026-94001"));

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

        var device = PosDevice.Create(
            deviceId,
            tenantId,
            outletId,
            "POS-01",
            "Front POS Device",
            "TABLET",
            "ACTIVE",
            null,
            now);

        device.PairForActivation(
            "Front POS Device",
            "TABLET",
            "web",
            "dev",
            DeviceFingerprintHasher.Hash(deviceFingerprint),
            userId,
            now);

        dbContext.PosDevices.Add(device);
        dbContext.Entry(device).Property(nameof(PosDevice.IsTrusted)).CurrentValue = isTrusted;

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
        new(dbContext, new PassthroughMediaReadUrlResolver(), NullLogger<PosHomeDashboardRepository>.Instance);

    private sealed class PassthroughMediaReadUrlResolver : IMediaReadUrlResolver
    {
        public string? ResolveReadUrl(string? mediaPublicUrl) => mediaPublicUrl;

        public string? ResolveReadUrl(string? containerName, string? storageKey, string? mediaPublicUrl) =>
            mediaPublicUrl ?? storageKey;
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
