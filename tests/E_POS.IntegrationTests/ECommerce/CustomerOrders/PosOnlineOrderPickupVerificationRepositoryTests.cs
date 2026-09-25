using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.ECommerce.CustomerOrders;

public sealed class PosOnlineOrderPickupVerificationRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task MarkReady_IssuesASingleUsePickupCodeThatNeverExpires()
    {
        await using var db = CreateDbContext();
        var fixture = PosOnlineOrderPackingRepositoryTests.SeedPackableAggregate(
            db, requested: 1, picked: 1, version: 3);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new PosOnlineOrderPackingRepository(db);
        Assert.True((await repository.PackAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderPackRequest { ExpectedVersion = 3 },
            Now, CancellationToken.None)).IsSuccess);
        db.ChangeTracker.Clear();

        var readyAt = Now.AddMinutes(5);
        var result = await repository.MarkReadyAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderReadyRequest { ExpectedVersion = 4 },
            readyAt, CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorCode);
        db.ChangeTracker.Clear();
        var pickup = await db.PickupOrders.SingleAsync();
        Assert.False(string.IsNullOrEmpty(pickup.PickupQrTokenHash));
        Assert.Equal(1, pickup.PickupQrVersion);
        Assert.Null(pickup.PickupQrExpiresAt);
        Assert.Equal(0, pickup.FailedVerificationAttempts);
    }

    [Fact]
    public async Task Verify_CorrectCode_TransitionsToVerifiedAndBurnsTheCode()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedReadyOrderAsync(db);
        var repository = new PosOnlineOrderPickupVerificationRepository(db);

        var result = await repository.VerifyAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.OrderId,
            fixture.PickupCode, Now.AddMinutes(10), CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorCode);
        Assert.Equal("VERIFIED", result.Value!.PickupStatus);
        db.ChangeTracker.Clear();
        var pickup = await db.PickupOrders.SingleAsync();
        Assert.Equal("VERIFIED", pickup.PickupStatus);
        Assert.Null(pickup.PickupQrTokenHash);
        Assert.Equal("QR", pickup.VerificationMethod);
        Assert.Equal(fixture.UserId, pickup.VerifiedByTenantUserId);
        Assert.Equal(Now.AddMinutes(10), pickup.VerifiedAt);
        Assert.Single(await db.PickupOrderEvents.ToListAsync(),
            x => x.EventType == PosOnlineOrderPickupVerificationRepository.VerifiedEvent);
    }

    [Fact]
    public async Task Verify_WrongCode_IncrementsAttemptsAndReportsRemaining()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedReadyOrderAsync(db);
        var repository = new PosOnlineOrderPickupVerificationRepository(db);

        var result = await repository.VerifyAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.OrderId,
            "not-the-real-code", Now.AddMinutes(1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.pickup_code_mismatch", result.ErrorCode);
        Assert.Equal(2, result.RemainingAttempts);
        db.ChangeTracker.Clear();
        var pickup = await db.PickupOrders.SingleAsync();
        Assert.Equal("READY", pickup.PickupStatus);
        Assert.Equal(1, pickup.FailedVerificationAttempts);
        Assert.False(string.IsNullOrEmpty(pickup.PickupQrTokenHash));
    }

    [Fact]
    public async Task Verify_ThreeWrongAttempts_LocksOutFurtherAttempts()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedReadyOrderAsync(db);
        var repository = new PosOnlineOrderPickupVerificationRepository(db);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var failed = await repository.VerifyAsync(
                fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.OrderId,
                "wrong-code", Now.AddMinutes(attempt), CancellationToken.None);
            Assert.False(failed.IsSuccess);
            db.ChangeTracker.Clear();
        }

        var lockedOut = await repository.VerifyAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.OrderId,
            fixture.PickupCode, Now.AddMinutes(5), CancellationToken.None);

        Assert.False(lockedOut.IsSuccess);
        Assert.Equal("online_orders.pickup_verification_locked", lockedOut.ErrorCode);
        db.ChangeTracker.Clear();
        Assert.Equal(3, (await db.PickupOrders.SingleAsync()).FailedVerificationAttempts);
        Assert.Single(await db.PickupOrderEvents.ToListAsync(),
            x => x.EventType == PosOnlineOrderPickupVerificationRepository.VerificationLockedEvent);
    }

    [Fact]
    public async Task Verify_LongAfterReady_StillAccepted()
    {
        await using var db = CreateDbContext();
        // Issued (and never expires) long before this verification attempt — a customer taking
        // days or weeks to collect must not find their code stale when they finally show up.
        var fixture = await SeedReadyOrderAsync(db, readyAt: Now.AddDays(-30));
        var repository = new PosOnlineOrderPickupVerificationRepository(db);

        var result = await repository.VerifyAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.OrderId,
            fixture.PickupCode, Now, CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorCode);
        db.ChangeTracker.Clear();
        Assert.Equal("VERIFIED", (await db.PickupOrders.SingleAsync()).PickupStatus);
    }

    [Fact]
    public async Task Collect_AfterVerify_MarksCollectedAndCompletesOrder()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedReadyOrderAsync(db);
        var repository = new PosOnlineOrderPickupVerificationRepository(db);

        var verified = await repository.VerifyAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.OrderId,
            fixture.PickupCode, Now.AddMinutes(1), CancellationToken.None);
        Assert.True(verified.IsSuccess, verified.ErrorCode);
        db.ChangeTracker.Clear();

        var collected = await repository.CollectAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.OrderId,
            Now.AddMinutes(2), CancellationToken.None);

        Assert.True(collected.IsSuccess, collected.ErrorCode);
        Assert.Equal("COLLECTED", collected.Value!.PickupStatus);
        Assert.Equal("COMPLETED", collected.Value.OrderStatus);
        db.ChangeTracker.Clear();
        var pickup = await db.PickupOrders.SingleAsync();
        Assert.Equal("COLLECTED", pickup.PickupStatus);
        Assert.Equal(Now.AddMinutes(2), pickup.CollectedAt);
        var order = await db.SalesOrders.SingleAsync();
        Assert.Equal("COMPLETED", order.Status);
        Assert.Equal("COLLECTED", order.FulfillmentStatus);
        Assert.Equal(Now.AddMinutes(2), order.CompletedAt);
        Assert.Single(await db.PickupOrderEvents.ToListAsync(),
            x => x.EventType == PosOnlineOrderPickupVerificationRepository.CollectedEvent);
    }

    [Fact]
    public async Task Collect_WithoutVerifyingFirst_Rejected()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedReadyOrderAsync(db);
        var repository = new PosOnlineOrderPickupVerificationRepository(db);

        var result = await repository.CollectAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.OrderId,
            Now.AddMinutes(1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.invalid_state", result.ErrorCode);
        db.ChangeTracker.Clear();
        Assert.Equal("READY", (await db.PickupOrders.SingleAsync()).PickupStatus);
        Assert.NotEqual("COMPLETED", (await db.SalesOrders.SingleAsync()).Status);
    }

    private static async Task<ReadyFixture> SeedReadyOrderAsync(
        EPosDbContext db, DateTimeOffset? readyAt = null)
    {
        var packing = PosOnlineOrderPackingRepositoryTests.SeedPackableAggregate(
            db, requested: 1, picked: 1, version: 3);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repository = new PosOnlineOrderPackingRepository(db);
        var packResult = await repository.PackAsync(
            packing.TenantId, packing.UserId, packing.OutletId, packing.Order.Id,
            new PosOnlineOrderPackRequest { ExpectedVersion = 3 },
            Now, CancellationToken.None);
        Assert.True(packResult.IsSuccess, packResult.ErrorCode);
        db.ChangeTracker.Clear();

        var readyResult = await repository.MarkReadyAsync(
            packing.TenantId, packing.UserId, packing.OutletId, packing.Order.Id,
            new PosOnlineOrderReadyRequest { ExpectedVersion = 4 },
            readyAt ?? Now, CancellationToken.None);
        Assert.True(readyResult.IsSuccess, readyResult.ErrorCode);
        db.ChangeTracker.Clear();

        var pickupCode = (await db.PickupOrders.SingleAsync()).PickupQrTokenHash!;
        return new ReadyFixture(
            packing.TenantId, packing.UserId, packing.OutletId, packing.Order.Id, pickupCode);
    }

    private static EPosDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed record ReadyFixture(
        Guid TenantId, Guid UserId, Guid OutletId, Guid OrderId, string PickupCode);
}
