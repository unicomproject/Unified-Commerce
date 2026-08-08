using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Modules.Tenant.POSOperations.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace E_POS.IntegrationTests.POSOperations;

/// <summary>
/// Exercises PosHoldRepository.GetActiveHoldsAsync against an in-memory EF Core
/// context to prove active holds are scoped to the till resolved from the caller's
/// trusted device + open till session (never a client-supplied tillId, never
/// OpenedByTenantUserId), mirroring PosTillSessionRepositoryTests' InMemory approach.
/// </summary>
public sealed class PosHoldRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetActiveHoldsAsync_OnlyReturnsHoldsOnDeviceResolvedTill()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var ownTillId = Guid.NewGuid();
        var otherTillId = Guid.NewGuid();
        var salesChannelId = Guid.NewGuid();

        SeedDeviceContext(dbContext, tenantId, outletId, ownTillId, deviceId, userId);
        // The user's own, currently-open till session is on ownTillId; a hold parked
        // under a different till (otherTillId) must never show up in their list, even
        // though both holds were parked by the same tenant user.
        dbContext.TillSessions.Add(TillSession.Open(
            Guid.NewGuid(), tenantId, outletId, ownTillId, "TS-0001",
            DateOnly.FromDateTime(Now.UtcDateTime), userId, deviceId, 100m, "LKR", null, Now));

        var ownTillOrderId = Guid.NewGuid();
        var ownTillHoldId = Guid.NewGuid();
        dbContext.SalesOrders.Add(SalesOrder.CreateHeldPosSale(
            ownTillOrderId, tenantId, "SO-000001", "POS_HOLD:own", salesChannelId,
            null, null, ownTillId, Guid.NewGuid(), null, "LKR", false,
            100m, 0m, 0m, 100m, "Own till hold", userId,
            DateOnly.FromDateTime(Now.UtcDateTime), Now));
        dbContext.PosOrderHolds.Add(PosOrderHold.Create(
            ownTillHoldId, tenantId, "PS-2026-00001", ownTillOrderId, "Own till hold",
            userId, Now, Now.AddHours(24)));

        var otherTillOrderId = Guid.NewGuid();
        var otherTillHoldId = Guid.NewGuid();
        dbContext.SalesOrders.Add(SalesOrder.CreateHeldPosSale(
            otherTillOrderId, tenantId, "SO-000002", "POS_HOLD:other", salesChannelId,
            null, null, otherTillId, Guid.NewGuid(), null, "LKR", false,
            200m, 0m, 0m, 200m, "Other till hold", userId,
            DateOnly.FromDateTime(Now.UtcDateTime), Now));
        dbContext.PosOrderHolds.Add(PosOrderHold.Create(
            otherTillHoldId, tenantId, "PS-2026-00002", otherTillOrderId, "Other till hold",
            userId, Now, Now.AddHours(24)));

        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var result = await repository.GetActiveHoldsAsync(
            tenantId, userId, new PosHoldListQueryDto(deviceId), Now, default);

        Assert.True(result.IsSuccess, result.ErrorCode);
        var hold = Assert.Single(result.Holds!);
        Assert.Equal(ownTillHoldId, hold.HoldId);
        Assert.Equal(ownTillId, hold.TillId);
    }

    [Fact]
    public async Task GetActiveHoldsAsync_WhenNoOpenTillSessionForDevice_ReturnsTillSessionError()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var tillId = Guid.NewGuid();

        // Device is trusted and assigned, but no till session is currently open.
        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var result = await repository.GetActiveHoldsAsync(
            tenantId, userId, new PosHoldListQueryDto(deviceId), Now, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("till_session.not_found", result.ErrorCode);
        Assert.Null(result.Holds);
    }

    [Fact]
    public async Task GetActiveHoldsAsync_WhenDeviceUnknown_ReturnsDeviceNotFoundError()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var repository = CreateRepository(dbContext);
        var result = await repository.GetActiveHoldsAsync(
            tenantId, userId, new PosHoldListQueryDto(Guid.NewGuid()), Now, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("till_session.device_not_found", result.ErrorCode);
        Assert.Null(result.Holds);
    }

    [Fact]
    public async Task GetActiveHoldsAsync_AppliesScopeBeforeAggregatesAndPagination()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid();
        var olderSessionId = Guid.NewGuid();
        var salesChannelId = Guid.NewGuid();
        var businessDate = new DateOnly(2026, 8, 6);

        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId);
        dbContext.TillSessions.Add(TillSession.Open(
            currentSessionId, tenantId, outletId, tillId, "TS-0002",
            businessDate, userId, deviceId, 100m, "LKR", null, Now));

        AddHold("00001", currentSessionId, businessDate, 100m, Now.AddMinutes(-3));
        AddHold("00002", olderSessionId, businessDate, 200m, Now.AddMinutes(-2));
        AddHold("00003", olderSessionId, businessDate.AddDays(-1), 300m, Now.AddMinutes(-1));
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var today = await repository.GetActiveHoldsAsync(
            tenantId, userId,
            new PosHoldListQueryDto(deviceId, PosHoldListScopes.Today, 1, 1),
            Now, default);
        var shift = await repository.GetActiveHoldsAsync(
            tenantId, userId,
            new PosHoldListQueryDto(deviceId, PosHoldListScopes.CurrentShift, 1, 25),
            Now, default);
        var all = await repository.GetActiveHoldsAsync(
            tenantId, userId,
            new PosHoldListQueryDto(deviceId, PosHoldListScopes.AllActive, 2, 2),
            Now, default);

        Assert.True(today.IsSuccess, today.ErrorCode);
        Assert.Single(today.Holds!);
        Assert.Equal("PS-2026-00002", today.Holds![0].HoldNumber);
        Assert.Equal(2, today.TotalCount);
        Assert.Equal(300, today.TotalValue);
        Assert.Equal("LKR", today.Currency);

        Assert.True(shift.IsSuccess, shift.ErrorCode);
        Assert.Single(shift.Holds!);
        Assert.Equal("PS-2026-00001", shift.Holds![0].HoldNumber);
        Assert.Equal(1, shift.TotalCount);
        Assert.Equal(100, shift.TotalValue);

        Assert.True(all.IsSuccess, all.ErrorCode);
        Assert.Single(all.Holds!);
        Assert.Equal("PS-2026-00001", all.Holds![0].HoldNumber);
        Assert.Equal(3, all.TotalCount);
        Assert.Equal(600, all.TotalValue);

        void AddHold(
            string suffix,
            Guid tillSessionId,
            DateOnly orderBusinessDate,
            decimal total,
            DateTimeOffset heldAt)
        {
            var saleId = Guid.NewGuid();
            dbContext.SalesOrders.Add(SalesOrder.CreateHeldPosSale(
                saleId, tenantId, $"SO-{suffix}", $"POS_HOLD:{suffix}", salesChannelId,
                null, null, tillId, tillSessionId, null, "LKR", false,
                total, 0m, 0m, total, null, userId, orderBusinessDate, heldAt));
            dbContext.PosOrderHolds.Add(PosOrderHold.Create(
                Guid.NewGuid(), tenantId, $"PS-2026-{suffix}", saleId, null,
                userId, heldAt, Now.AddHours(24)));
        }
    }

    private static PosHoldRepository CreateRepository(EPosDbContext dbContext)
    {
        var tillSessionRepository = new PosTillSessionRepository(
            dbContext, new CodeSequenceRepository(dbContext), NullLogger<PosTillSessionRepository>.Instance);
        var checkoutRepository = new PosCheckoutRepository(
            dbContext, tillSessionRepository, new StubReceiptTemplateResolutionService(), null);
        return new PosHoldRepository(dbContext, checkoutRepository, tillSessionRepository);
    }

    private sealed class StubReceiptTemplateResolutionService : IReceiptTemplateResolutionService
    {
        public Task<ResolvedReceiptTemplateDto?> ResolveTemplateAsync(
            Guid tenantId, Guid outletId, Guid tillId, Guid deviceId, CancellationToken cancellationToken) =>
            Task.FromResult<ResolvedReceiptTemplateDto?>(null);
    }

    private static void SeedDeviceContext(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid outletId,
        Guid tillId,
        Guid deviceId,
        Guid userId)
    {
        dbContext.Tenants.Add(Tenant.Create(
            tenantId, "DEV-002", "dev-002", "Test Tenant", "active",
            "LKR", "UTC", null, null, Now));

        dbContext.Outlets.Add(Outlet.Create(
            outletId, tenantId, "Main Outlet", "MAIN-01", "ACTIVE",
            "STORE", "UTC", true, null, null, null, Now));

        dbContext.Tills.Add(Till.Create(
            tillId, tenantId, outletId, "Front Till 01", "Front", 1,
            $"FRONT-{tillId:N}", "STANDARD", 0m, "LKR", true, "ACTIVE", null, Now));

        var device = PosDevice.Create(
            deviceId, tenantId, outletId, $"POS-{deviceId:N}", "Front POS Device",
            "TABLET", "ACTIVE", null, Now);
        typeof(PosDevice).GetProperty(nameof(PosDevice.IsTrusted))!.SetValue(device, true);
        dbContext.PosDevices.Add(device);

        dbContext.TillDeviceAssignments.Add(
            TillDeviceAssignment.Create(Guid.NewGuid(), tenantId, outletId, tillId, deviceId, userId, Now));
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
