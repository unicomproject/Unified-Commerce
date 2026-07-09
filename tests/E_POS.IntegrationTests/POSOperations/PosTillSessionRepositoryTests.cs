using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Modules.Tenant.POSOperations.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;

namespace E_POS.IntegrationTests.POSOperations;

public sealed class PosTillSessionRepositoryTests
{
    [Fact]
    public async Task ResolveCurrentSessionAsync_WhenOpenSessionExists_ReturnsSnapshot()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);

        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, now, isTrusted: true);
        dbContext.TillSessions.Add(TillSession.Open(
            sessionId,
            tenantId,
            outletId,
            tillId,
            "TS-0001",
            DateOnly.FromDateTime(now.UtcDateTime),
            userId,
            deviceId,
            150m,
            "LKR",
            "Morning shift",
            now));
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var result = await repository.ResolveCurrentSessionAsync(tenantId, deviceId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(sessionId, result.Snapshot!.SessionId);
        Assert.Equal(150m, result.Snapshot.OpeningFloat);
        Assert.Equal("Morning shift", result.Snapshot.OpeningNote);
    }

    [Fact]
    public async Task ResolveCurrentSessionAsync_WhenNoOpenSession_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);

        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, now, isTrusted: true);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var result = await repository.ResolveCurrentSessionAsync(tenantId, deviceId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("till_session.not_found", result.ErrorCode);
    }

    [Fact]
    public async Task ResolveCurrentSessionAsync_WhenDeviceNotTrusted_ReturnsFailure()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);

        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, now, isTrusted: false);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var result = await repository.ResolveCurrentSessionAsync(tenantId, deviceId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("till_session.device_not_trusted", result.ErrorCode);
    }

    [Fact]
    public async Task OpenTillAsync_WhenValidRequest_CreatesOpenSession()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);

        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, now, isTrusted: true);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var result = await repository.OpenTillAsync(
            tenantId,
            userId,
            new OpenTillCommand(deviceId, tillId, 200m, "Morning shift"),
            now,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(tillId, result.Snapshot!.TillId);
        Assert.Equal(200m, result.Snapshot.OpeningFloat);
        Assert.Equal("OPEN", result.Snapshot.Status);

        var savedSession = await dbContext.TillSessions.SingleAsync();
        Assert.Equal("TS-0001", savedSession.SessionNumber);
        Assert.Equal("Morning shift", savedSession.OpeningNote);
    }

    [Fact]
    public async Task OpenTillAsync_WhenTillMismatch_ReturnsFailure()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);

        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, now, isTrusted: true);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var result = await repository.OpenTillAsync(
            tenantId,
            userId,
            new OpenTillCommand(deviceId, Guid.NewGuid(), 0m, null),
            now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("till_session.till_mismatch", result.ErrorCode);
    }

    [Fact]
    public async Task OpenTillAsync_WhenSessionAlreadyOpen_ReturnsConflict()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);

        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, now, isTrusted: true);
        dbContext.TillSessions.Add(TillSession.Open(
            Guid.NewGuid(),
            tenantId,
            outletId,
            tillId,
            "TS-0001",
            DateOnly.FromDateTime(now.UtcDateTime),
            userId,
            deviceId,
            0m,
            "LKR",
            null,
            now));
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var result = await repository.OpenTillAsync(
            tenantId,
            userId,
            new OpenTillCommand(deviceId, tillId, 100m, null),
            now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("till_session.already_open", result.ErrorCode);
    }

    [Fact]
    public async Task CloseTillAsync_WhenValidRequest_ClosesOpenSession()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 18, 0, 0, TimeSpan.Zero);

        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, now, isTrusted: true);
        dbContext.TillSessions.Add(TillSession.Open(
            sessionId,
            tenantId,
            outletId,
            tillId,
            "TS-0001",
            DateOnly.FromDateTime(now.UtcDateTime),
            userId,
            deviceId,
            150m,
            "LKR",
            "Morning shift",
            now.AddHours(-8)));
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var result = await repository.CloseTillAsync(
            tenantId,
            userId,
            new CloseTillCommand(deviceId, tillId, 480m, 480m, null, "End of shift"),
            now,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Snapshot);
        Assert.Equal("CLOSED", result.Snapshot!.Status);
        Assert.Equal(480m, result.Snapshot.CountedCash);
        Assert.Equal(0m, result.Snapshot.CashDifference);

        var savedSession = await dbContext.TillSessions.SingleAsync();
        Assert.NotNull(savedSession.ClosedAt);
        Assert.Equal("CLOSED", savedSession.Status);

        var closedEvent = await dbContext.TillSessionEvents.SingleAsync();
        Assert.Equal("CLOSED", closedEvent.EventType);
    }

    [Fact]
    public async Task CloseTillAsync_WhenMismatchWithoutReason_ReturnsFailure()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 18, 0, 0, TimeSpan.Zero);

        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, now, isTrusted: true);
        dbContext.TillSessions.Add(TillSession.Open(
            Guid.NewGuid(),
            tenantId,
            outletId,
            tillId,
            "TS-0001",
            DateOnly.FromDateTime(now.UtcDateTime),
            userId,
            deviceId,
            150m,
            "LKR",
            null,
            now.AddHours(-8)));
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var result = await repository.CloseTillAsync(
            tenantId,
            userId,
            new CloseTillCommand(deviceId, tillId, 500m, 480m, null, null),
            now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("till_session.mismatch_reason_required", result.ErrorCode);
    }

    private static PosTillSessionRepository CreateRepository(EPosDbContext dbContext) =>
        new(
            dbContext,
            new CodeSequenceRepository(dbContext),
            NullLogger<PosTillSessionRepository>.Instance);

    private static void SeedDeviceContext(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid outletId,
        Guid tillId,
        Guid deviceId,
        Guid userId,
        DateTimeOffset now,
        bool isTrusted)
    {
        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            "DEV-001",
            "dev-001",
            "Test Tenant",
            "active",
            "LKR",
            "UTC",
            null,
            null,
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

        if (isTrusted)
        {
            typeof(PosDevice).GetProperty(nameof(PosDevice.IsTrusted))!
                .SetValue(device, true);
        }

        dbContext.PosDevices.Add(device);
        dbContext.TillDeviceAssignments.Add(
            TillDeviceAssignment.Create(Guid.NewGuid(), tenantId, outletId, tillId, deviceId, userId, now));
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
