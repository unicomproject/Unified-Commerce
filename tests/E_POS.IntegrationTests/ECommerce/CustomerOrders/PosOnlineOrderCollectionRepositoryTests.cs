using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.ECommerce.CustomerOrders;

public sealed class PosOnlineOrderCollectionRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Validate_ZeroMutation_ReturnsCanCollectAndExpectedVersion()
    {
        await using var db = CreateDbContext();
        var fixture = SeedReadyAggregate(db, paid: true);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repository = new PosOnlineOrderCollectionRepository(db);
        var result = await repository.ValidateQrAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Token,
            Now.AddMinutes(1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.Order.Id, result.Value!.OrderId);
        Assert.True(result.Value.CanCollect);
        Assert.False(result.Value.CanTakePayment);
        Assert.Equal(fixture.Fulfillment.RowVersion, result.Value.ExpectedVersion);

        db.ChangeTracker.Clear();
        var pickup = await db.PickupOrders.SingleAsync();
        Assert.Equal("READY", pickup.PickupStatus);
        Assert.Null(pickup.CollectedAt);
        Assert.Empty(await db.PickupOrderEvents.ToListAsync());
        Assert.Empty(await db.FulfillmentOrderEvents
            .Where(x => x.EventType == PosOnlineOrderCollectionRepository.FulfilledEvent)
            .ToListAsync());
    }

    [Fact]
    public async Task Complete_SetsCollectedFulfilledAndCompleted()
    {
        await using var db = CreateDbContext();
        var fixture = SeedReadyAggregate(db, paid: true);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repository = new PosOnlineOrderCollectionRepository(db);
        var result = await repository.CompleteAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            fixture.Fulfillment.RowVersion, Now.AddMinutes(2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.AlreadyCollected);
        Assert.Equal("COLLECTED", result.Value.PickupStatus);
        Assert.Equal("FULFILLED", result.Value.FulfillmentStatus);
        Assert.Equal("COMPLETED", result.Value.SalesOrderStatus);
        Assert.Equal("COLLECTED", result.Value.SalesFulfillmentStatus);

        db.ChangeTracker.Clear();
        var pickup = await db.PickupOrders.SingleAsync();
        var fulfillment = await db.FulfillmentOrders.SingleAsync();
        var order = await db.SalesOrders.SingleAsync();
        Assert.Equal("COLLECTED", pickup.PickupStatus);
        Assert.NotNull(pickup.CollectedAt);
        Assert.Equal("FULFILLED", fulfillment.FulfillmentStatus);
        Assert.NotNull(fulfillment.FulfilledAt);
        Assert.Equal(fixture.Fulfillment.RowVersion + 1, fulfillment.RowVersion);
        Assert.Equal("COMPLETED", order.Status);
        Assert.Equal("COLLECTED", order.FulfillmentStatus);
        Assert.NotNull(order.CompletedAt);
        Assert.Contains(await db.PickupOrderEvents.ToListAsync(),
            x => x.EventType == PosOnlineOrderCollectionRepository.PickupCollectedEvent);
        Assert.Contains(await db.FulfillmentOrderEvents.ToListAsync(),
            x => x.EventType == PosOnlineOrderCollectionRepository.FulfilledEvent);
    }

    [Fact]
    public async Task Complete_Unpaid_RequiresPayment()
    {
        await using var db = CreateDbContext();
        var fixture = SeedReadyAggregate(db, paid: false);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await new PosOnlineOrderCollectionRepository(db).CompleteAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            fixture.Fulfillment.RowVersion, Now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.collection.payment_required", result.ErrorCode);
        Assert.Null((await db.PickupOrders.SingleAsync()).CollectedAt);
    }

    [Fact]
    public async Task Complete_Idempotent_WhenAlreadyCollected()
    {
        await using var db = CreateDbContext();
        var fixture = SeedReadyAggregate(db, paid: true);
        fixture.Pickup.MarkCollected(Now);
        fixture.Fulfillment.MarkFulfilled(fixture.UserId, fixture.Fulfillment.RowVersion, Now);
        fixture.Order.ApplyPosCollected(fixture.UserId, Now);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await new PosOnlineOrderCollectionRepository(db).CompleteAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            99, Now.AddMinutes(5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.AlreadyCollected);
        Assert.Empty(await db.PickupOrderEvents
            .Where(x => x.EventType == PosOnlineOrderCollectionRepository.PickupCollectedEvent)
            .ToListAsync());
    }

    [Fact]
    public async Task MarkReady_IssuesOneTimeTokenStoredVerbatimForRedisplay()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPackableForReady(db);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await new PosOnlineOrderPackingRepository(db).MarkReadyAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderReadyRequest { ExpectedVersion = fixture.Fulfillment.RowVersion },
            Now, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Command!.CollectionQrToken));

        db.ChangeTracker.Clear();
        var pickup = await db.PickupOrders.SingleAsync();
        Assert.Equal("READY", pickup.PickupStatus);
        Assert.NotNull(pickup.PickupQrTokenHash);
        Assert.Equal(1, pickup.PickupQrVersion);
        Assert.Equal(Now.AddDays(7), pickup.PickupQrExpiresAt);
        // Stored as-is (not hashed): the customer's order page must be able to
        // redisplay this exact value, and validation compares it verbatim.
        Assert.Equal(result.Command.CollectionQrToken, pickup.PickupQrTokenHash);
        Assert.Null(pickup.CollectedAt);
    }

    private static CollectionFixture SeedReadyAggregate(EPosDbContext db, bool paid)
    {
        var baseFixture = SeedPackableForReady(db);
        baseFixture.Fulfillment.MarkReady(baseFixture.UserId, baseFixture.Fulfillment.RowVersion, Now);
        baseFixture.Order.ApplyPosReadyForCollection(baseFixture.UserId, Now);
        baseFixture.Pickup.MarkReady(Now);

        const string token = "test-collection-token-chunk2";
        baseFixture.Pickup.IssueCollectionQr(token, 1, Now.AddDays(7), Now);

        if (paid)
            baseFixture.Order.ApplyPosCollectionPayment(baseFixture.Order.BalanceDue, baseFixture.UserId, Now);

        return new CollectionFixture(
            baseFixture.TenantId, baseFixture.UserId, baseFixture.OutletId,
            baseFixture.Order, baseFixture.Fulfillment, baseFixture.Pickup, token);
    }

    private static PackingReadyFixture SeedPackableForReady(EPosDbContext db)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        db.Tenants.Add(Tenant.Create(
            tenantId, "T-C", $"tenant-{tenantId:N}", "Tenant", "active", "LKR",
            "Asia/Colombo", null, null, Now));
        db.TenantUsers.Add(TenantUser.Create(
            userId, tenantId, $"{userId:N}@example.com", "Collector", null, null,
            "hash", "salt", "ACTIVE", "cashier", "cashier", null, Now,
            staffCode: $"STAFF-{userId:N}"));
        db.Outlets.Add(Outlet.Create(
            outletId, tenantId, "Main Store", "MAIN", "ACTIVE", "STORE",
            "Asia/Colombo", true, null, null, userId, Now));

        var order = SalesOrder.CreateClickAndCollect(
            Guid.NewGuid(), tenantId, "EC-COLLECT-1", "idem-collect-1", Guid.NewGuid(), Guid.NewGuid(),
            "CLICK_COLLECT", outletId, "MAIN", "Main Store", Guid.NewGuid(), "Test Customer",
            "customer@example.com", "+94110000000", "LKR", false, 2000m, 0m, 0m, 0m, 2000m,
            Now.AddHours(2), Now.AddHours(3), "Asia/Colombo", Now);
        var salesLine = SalesOrderLine.CreateForClickAndCollect(
            Guid.NewGuid(), tenantId, order.Id, 1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "SKU-1", "SKU-1", "Product One", "Blue / M", "EA", "Each", "STANDARD", "VARIANT",
            2m, 1000m, 2000m, 0m, 0m, false, Now);
        var methodOutlet = FulfillmentMethodOutlet.Create(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), outletId, null, null, null, "ACTIVE", Now);
        var location = InventoryLocation.Create(
            Guid.NewGuid(), tenantId, outletId, null, "PICK", "Picking Area", "STORAGE",
            false, false, false, false, "ACTIVE", userId, Now);
        var fulfillment = Create<FulfillmentOrder>();
        Set(fulfillment, nameof(fulfillment.Id), Guid.NewGuid());
        Set(fulfillment, nameof(fulfillment.TenantId), tenantId);
        Set(fulfillment, nameof(fulfillment.SalesOrderId), order.Id);
        Set(fulfillment, nameof(fulfillment.FulfillmentNumber), "FUL-COLLECT-1");
        Set(fulfillment, nameof(fulfillment.FulfillmentMethodOutletId), methodOutlet.Id);
        Set(fulfillment, nameof(fulfillment.SourceInventoryLocationId), location.Id);
        Set(fulfillment, nameof(fulfillment.FulfillmentStatus), "PACKED");
        Set(fulfillment, nameof(fulfillment.AssignedToTenantUserId), userId);
        Set(fulfillment, nameof(fulfillment.RowVersion), 5L);
        Set(fulfillment, nameof(fulfillment.PackedAt), Now);
        Set(fulfillment, nameof(fulfillment.CreatedAt), Now);
        Set(fulfillment, nameof(fulfillment.UpdatedAt), Now);
        var fulfillmentLine = Create<FulfillmentOrderLine>();
        Set(fulfillmentLine, nameof(fulfillmentLine.Id), Guid.NewGuid());
        Set(fulfillmentLine, nameof(fulfillmentLine.TenantId), tenantId);
        Set(fulfillmentLine, nameof(fulfillmentLine.FulfillmentOrderId), fulfillment.Id);
        Set(fulfillmentLine, nameof(fulfillmentLine.SalesOrderLineId), salesLine.Id);
        Set(fulfillmentLine, nameof(fulfillmentLine.RequestedQuantity), 2m);
        Set(fulfillmentLine, nameof(fulfillmentLine.PickedQuantity), 2m);
        Set(fulfillmentLine, nameof(fulfillmentLine.PackedQuantity), 2m);
        Set(fulfillmentLine, nameof(fulfillmentLine.LineStatus), "PACKED");
        Set(fulfillmentLine, nameof(fulfillmentLine.CreatedAt), Now);
        Set(fulfillmentLine, nameof(fulfillmentLine.UpdatedAt), Now);

        var pickup = Create<PickupOrder>();
        Set(pickup, nameof(pickup.Id), Guid.NewGuid());
        Set(pickup, nameof(pickup.TenantId), tenantId);
        Set(pickup, nameof(pickup.FulfillmentOrderId), fulfillment.Id);
        Set(pickup, nameof(pickup.PickupNumber), "PU-COLLECT-1");
        Set(pickup, nameof(pickup.PickupContactName), "Test Customer");
        Set(pickup, nameof(pickup.PickupStatus), "PENDING");
        Set(pickup, nameof(pickup.CreatedAt), Now);
        Set(pickup, nameof(pickup.UpdatedAt), Now);

        db.AddRange(order, salesLine, methodOutlet, location, fulfillment, fulfillmentLine, pickup);
        return new PackingReadyFixture(tenantId, userId, outletId, order, fulfillment, pickup);
    }

    private static T Create<T>() where T : class =>
        (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

    private static void Set<T>(T target, string property, object? value) where T : class =>
        typeof(T).GetProperty(property)!.SetValue(target, value);

    private static EPosDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed record PackingReadyFixture(
        Guid TenantId, Guid UserId, Guid OutletId,
        SalesOrder Order, FulfillmentOrder Fulfillment, PickupOrder Pickup);

    private sealed record CollectionFixture(
        Guid TenantId, Guid UserId, Guid OutletId,
        SalesOrder Order, FulfillmentOrder Fulfillment, PickupOrder Pickup,
        string Token);
}
