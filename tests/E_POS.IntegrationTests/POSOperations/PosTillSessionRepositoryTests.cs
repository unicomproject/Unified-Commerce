using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Tenant.Payment.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
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
            new CloseTillCommand(deviceId, tillId, 150m, 480m, null, "End of shift"),
            now,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Snapshot);
        Assert.Equal("CLOSED", result.Snapshot!.Status);
        Assert.Equal(150m, result.Snapshot.ExpectedCash);
        Assert.Equal(150m, result.Snapshot.CountedCash);
        Assert.Equal(0m, result.Snapshot.CashDifference);

        var savedSession = await dbContext.TillSessions.SingleAsync();
        Assert.NotNull(savedSession.ClosedAt);
        Assert.Equal("CLOSED", savedSession.Status);

        var closedEvent = await dbContext.TillSessionEvents.SingleAsync();
        Assert.Equal("CLOSED", closedEvent.EventType);

        var reconciliation = await dbContext.CashReconciliations.SingleAsync();
        Assert.Equal(sessionId, reconciliation.TillSessionId);
        Assert.Equal(150m, reconciliation.ExpectedCashAmount);
        Assert.Equal(150m, reconciliation.CountedCashAmount);
        Assert.Equal(0m, reconciliation.DifferenceAmount);
        Assert.Equal("SUBMITTED", reconciliation.ReconciliationStatus);
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

    [Fact]
    public async Task CurrentAndClose_UseSameAuthoritativeCashActivity_AndIgnoreClientExpectedCash()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var cashMethodId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 8, 12, 18, 0, 0, TimeSpan.Zero);

        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, now, true);
        dbContext.TillSessions.Add(TillSession.Open(
            sessionId, tenantId, outletId, tillId, "TS-0042",
            DateOnly.FromDateTime(now.UtcDateTime), userId, deviceId, 100m, "LKR", null, now.AddHours(-8)));
        dbContext.PaymentMethods.Add(PaymentMethod.Create(
            cashMethodId, tenantId, "CASH", "Cash", "CASH", true, true, 1, "ACTIVE", userId, now));
        dbContext.SalesPayments.Add(SalesPayment.CreateCompletedPosPayment(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), "PAY-0042", cashMethodId, tillId, sessionId,
            "LKR", 250m, 300m, 250m, 50m, "close-test", "hash", userId, now));
        dbContext.TillCashMovements.Add(TillCashMovement.CreateCashIn(
            Guid.NewGuid(), tenantId, sessionId, 40m, "LKR", "Float top-up", "CIN-1", userId, now));
        dbContext.TillCashMovements.Add(TillCashMovement.CreateCashOut(
            Guid.NewGuid(), tenantId, sessionId, 30m, "LKR", "Cash refund", "REF-1", userId, now));
        // Exchange persistence may mirror a CASH payment with a CASH_IN carrying the payment number.
        // The calculator must not count that same physical cash twice.
        dbContext.TillCashMovements.Add(TillCashMovement.CreateCashIn(
            Guid.NewGuid(), tenantId, sessionId, 250m, "LKR", "Mirrored payment", "PAY-0042", userId, now));
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var current = await repository.ResolveCurrentSessionAsync(tenantId, deviceId, CancellationToken.None);
        Assert.True(current.IsSuccess);
        Assert.Equal(360m, current.Snapshot!.ExpectedCash);

        var closed = await repository.CloseTillAsync(
            tenantId, userId,
            new CloseTillCommand(deviceId, tillId, 360m, 1m, null, "Complete"),
            now.AddMinutes(1), CancellationToken.None);

        Assert.True(closed.IsSuccess);
        Assert.Equal(360m, closed.Snapshot!.ExpectedCash);
        Assert.Equal(0m, closed.Snapshot.CashDifference);
        var reconciliation = await dbContext.CashReconciliations.SingleAsync();
        Assert.Equal(360m, reconciliation.ExpectedCashAmount);
        Assert.Contains("\"CashPayments\":250", reconciliation.CalculationDetailsJson);
    }

    [Theory]
    [InlineData("Made up reason", "till_session.invalid_mismatch_reason")]
    [InlineData(null, "till_session.mismatch_reason_required")]
    public async Task CloseTillAsync_WithVariance_RejectsMissingOrInvalidReason(string? reason, string expectedError)
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid(); var outletId = Guid.NewGuid(); var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid(); var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, now, true);
        dbContext.TillSessions.Add(TillSession.Open(Guid.NewGuid(), tenantId, outletId, tillId,
            "TS-0001", DateOnly.FromDateTime(now.UtcDateTime), userId, deviceId, 100m, "LKR", null, now));
        await dbContext.SaveChangesAsync();

        var result = await CreateRepository(dbContext).CloseTillAsync(
            tenantId, userId, new CloseTillCommand(deviceId, tillId, 90m, 90m, reason, null), now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedError, result.ErrorCode);
        Assert.Empty(dbContext.CashReconciliations);
        Assert.Empty(dbContext.TillSessionEvents);
    }

    [Fact]
    public async Task CloseTillAsync_WhenClosingNoteExceeds500_RejectsWithoutPersistence()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid(); var outletId = Guid.NewGuid(); var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid(); var userId = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, now, true);
        dbContext.TillSessions.Add(TillSession.Open(Guid.NewGuid(), tenantId, outletId, tillId,
            "TS-0001", DateOnly.FromDateTime(now.UtcDateTime), userId, deviceId, 0m, "LKR", null, now));
        await dbContext.SaveChangesAsync();

        var result = await CreateRepository(dbContext).CloseTillAsync(
            tenantId, userId, new CloseTillCommand(deviceId, tillId, 0m, null, null, new string('x', 501)), now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("till_session.closing_note_too_long", result.ErrorCode);
    }

    [Fact]
    public async Task CloseTillAsync_WhenCalledTwice_PersistsOneReconciliationAndOneClosedEvent()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid(); var outletId = Guid.NewGuid(); var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid(); var userId = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        SeedDeviceContext(dbContext, tenantId, outletId, tillId, deviceId, userId, now, true);
        dbContext.TillSessions.Add(TillSession.Open(Guid.NewGuid(), tenantId, outletId, tillId,
            "TS-0001", DateOnly.FromDateTime(now.UtcDateTime), userId, deviceId, 0m, "LKR", null, now));
        await dbContext.SaveChangesAsync();
        var repository = CreateRepository(dbContext);
        var command = new CloseTillCommand(deviceId, tillId, 0m, 999m, null, null);

        var first = await repository.CloseTillAsync(tenantId, userId, command, now, CancellationToken.None);
        var second = await repository.CloseTillAsync(tenantId, userId, command, now.AddSeconds(1), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal("till_session.already_closed", second.ErrorCode);
        Assert.Single(dbContext.CashReconciliations);
        Assert.Single(dbContext.TillSessionEvents.Where(x => x.EventType == "CLOSED"));
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
