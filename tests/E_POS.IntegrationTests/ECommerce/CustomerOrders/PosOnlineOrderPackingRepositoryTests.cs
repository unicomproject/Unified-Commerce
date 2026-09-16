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

public sealed class PosOnlineOrderPackingRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReviewGet_ReturnsVersionPickedPackedAndCanPack()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPackableAggregate(db, requested: 2, picked: 2, version: 5);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var picking = new PosOnlineOrderPickingRepository(db);

        var result = await picking.GetAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            Now.AddMinutes(1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Picking!.FulfillmentVersion);
        Assert.True(result.Picking.CanPack);
        Assert.Equal(2, result.Picking.PickedUnits);
        Assert.Equal(0, Assert.Single(result.Picking.Lines).PackedQuantity);
    }

    [Fact]
    public async Task ReviewGet_CanPackFalse_WhenPendingUnitsRemain()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPackableAggregate(db, requested: 2, picked: 1, version: 5);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await new PosOnlineOrderPickingRepository(db).GetAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            Now, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Picking!.CanPack);
    }

    [Fact]
    public async Task ReviewGet_CancelledQuantity_DoesNotBlockCanPack()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPackableAggregate(db, requested: 3, picked: 2, version: 4, cancelled: 1);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await new PosOnlineOrderPickingRepository(db).GetAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            Now, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Picking!.CanPack);
        Assert.Equal(0, Assert.Single(result.Picking.Lines).RemainingQuantity);
    }

    [Fact]
    public async Task Pack_Success_WritesPackedQuantityEventAndVersion()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPackableAggregate(db, requested: 2, picked: 2, version: 5);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new PosOnlineOrderPackingRepository(db);

        var result = await repository.PackAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderPackRequest { ExpectedVersion = 5, PackingNote = "Handle with care" },
            Now.AddMinutes(1), CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorCode);
        Assert.Equal("PACKED", result.Command!.Status);
        Assert.Equal(6, result.Command.FulfillmentVersion);
        Assert.False(result.Command.CanPack);
        db.ChangeTracker.Clear();
        var line = await db.FulfillmentOrderLines.SingleAsync();
        Assert.Equal(2, line.PackedQuantity);
        Assert.Equal(fixture.UserId, line.PackedByTenantUserId);
        var fulfillment = await db.FulfillmentOrders.SingleAsync();
        Assert.Equal("PACKED", fulfillment.FulfillmentStatus);
        Assert.Equal(Now.AddMinutes(1), fulfillment.PackedAt);
        Assert.Null(fulfillment.ReadyAt);
        Assert.Equal(6, fulfillment.RowVersion);
        var packEvent = Assert.Single(await db.FulfillmentOrderEvents.ToListAsync());
        Assert.Equal(PosOnlineOrderPackingRepository.PackedEvent, packEvent.EventType);
        Assert.Equal("Handle with care", packEvent.EventNote);
        Assert.Equal(fixture.UserId, packEvent.EventByTenantUserId);
        Assert.Equal(Now.AddMinutes(1), packEvent.EventAt);
        Assert.Equal("PENDING", (await db.SalesOrders.SingleAsync()).FulfillmentStatus);
    }

    [Fact]
    public async Task Pack_CanPackFalse_RejectedWithZeroEvents()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPackableAggregate(db, requested: 2, picked: 1, version: 5);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await new PosOnlineOrderPackingRepository(db).PackAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderPackRequest { ExpectedVersion = 5 },
            Now.AddMinutes(1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.not_packable", result.ErrorCode);
        db.ChangeTracker.Clear();
        Assert.Equal(0, (await db.FulfillmentOrderLines.SingleAsync()).PackedQuantity);
        Assert.Equal(5, (await db.FulfillmentOrders.SingleAsync()).RowVersion);
        Assert.Empty(await db.FulfillmentOrderEvents.ToListAsync());
    }

    [Fact]
    public async Task Pack_TerminalReady_Rejected()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPackableAggregate(db, requested: 1, picked: 1, version: 8, status: "READY");
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await new PosOnlineOrderPackingRepository(db).PackAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderPackRequest { ExpectedVersion = 8 },
            Now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.invalid_state", result.ErrorCode);
        Assert.Empty(await db.FulfillmentOrderEvents.ToListAsync());
    }

    [Fact]
    public async Task Pack_StaleVersion_ConflictsWithoutMutation()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPackableAggregate(db, requested: 1, picked: 1, version: 5);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await new PosOnlineOrderPackingRepository(db).PackAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderPackRequest { ExpectedVersion = 4 },
            Now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.concurrency_conflict", result.ErrorCode);
        db.ChangeTracker.Clear();
        Assert.Equal(0, (await db.FulfillmentOrderLines.SingleAsync()).PackedQuantity);
        Assert.Equal(5, (await db.FulfillmentOrders.SingleAsync()).RowVersion);
        Assert.Empty(await db.FulfillmentOrderEvents.ToListAsync());
    }

    [Fact]
    public async Task Pack_DuplicateAndTwoCashier_ExactlyOneSuccess()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPackableAggregate(db, requested: 1, picked: 1, version: 10);
        var secondUserId = Guid.NewGuid();
        db.TenantUsers.Add(TenantUser.Create(
            secondUserId, fixture.TenantId, $"{secondUserId:N}@example.com", "Second Packer",
            null, null, "hash", "salt", "ACTIVE", "cashier", "cashier", null, Now,
            staffCode: $"STAFF-{secondUserId:N}"));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new PosOnlineOrderPackingRepository(db);
        var request = new PosOnlineOrderPackRequest { ExpectedVersion = 10 };

        var first = await repository.PackAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            request, Now.AddMinutes(1), CancellationToken.None);
        db.ChangeTracker.Clear();
        var duplicate = await repository.PackAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            request, Now.AddMinutes(2), CancellationToken.None);
        db.ChangeTracker.Clear();
        var secondCashier = await repository.PackAsync(
            fixture.TenantId, secondUserId, fixture.OutletId, fixture.Order.Id,
            request, Now.AddMinutes(3), CancellationToken.None);

        Assert.True(first.IsSuccess, first.ErrorCode);
        Assert.False(duplicate.IsSuccess);
        Assert.False(secondCashier.IsSuccess);
        Assert.Contains(duplicate.ErrorCode, new[]
        {
            "online_orders.concurrency_conflict", "online_orders.invalid_state"
        });
        Assert.Contains(secondCashier.ErrorCode, new[]
        {
            "online_orders.concurrency_conflict", "online_orders.invalid_state"
        });
        db.ChangeTracker.Clear();
        Assert.Equal(1, (await db.FulfillmentOrderLines.SingleAsync()).PackedQuantity);
        Assert.Equal(11, (await db.FulfillmentOrders.SingleAsync()).RowVersion);
        Assert.Single(await db.FulfillmentOrderEvents.ToListAsync(),
            x => x.EventType == PosOnlineOrderPackingRepository.PackedEvent);
    }

    [Fact]
    public async Task Pack_WrongTenantOrOutlet_DoesNotMutate()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPackableAggregate(db, requested: 1, picked: 1, version: 5);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new PosOnlineOrderPackingRepository(db);
        var request = new PosOnlineOrderPackRequest { ExpectedVersion = 5 };

        var wrongTenant = await repository.PackAsync(
            Guid.NewGuid(), fixture.UserId, fixture.OutletId, fixture.Order.Id,
            request, Now, CancellationToken.None);
        var wrongOutlet = await repository.PackAsync(
            fixture.TenantId, fixture.UserId, Guid.NewGuid(), fixture.Order.Id,
            request, Now, CancellationToken.None);

        Assert.False(wrongTenant.IsSuccess);
        Assert.False(wrongOutlet.IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Equal(0, (await db.FulfillmentOrderLines.SingleAsync()).PackedQuantity);
        Assert.Equal(5, (await db.FulfillmentOrders.SingleAsync()).RowVersion);
        Assert.Empty(await db.FulfillmentOrderEvents.ToListAsync());
    }

    [Fact]
    public async Task Ready_BeforePack_Rejected()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPackableAggregate(db, requested: 1, picked: 1, version: 5);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await new PosOnlineOrderPackingRepository(db).MarkReadyAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderReadyRequest { ExpectedVersion = 5 },
            Now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.not_readyable", result.ErrorCode);
        Assert.Empty(await db.FulfillmentOrderEvents.ToListAsync());
        Assert.Empty(await db.PickupOrderEvents.ToListAsync());
        Assert.Null((await db.PickupOrders.SingleAsync()).CollectedAt);
    }

    [Fact]
    public async Task Ready_AfterPack_TransitionsWithoutCollected()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPackableAggregate(db, requested: 2, picked: 2, version: 5);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new PosOnlineOrderPackingRepository(db);

        var packed = await repository.PackAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderPackRequest { ExpectedVersion = 5 },
            Now.AddMinutes(1), CancellationToken.None);
        db.ChangeTracker.Clear();
        var ready = await repository.MarkReadyAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderReadyRequest { ExpectedVersion = 6 },
            Now.AddMinutes(2), CancellationToken.None);

        Assert.True(packed.IsSuccess, packed.ErrorCode);
        Assert.True(ready.IsSuccess, ready.ErrorCode);
        Assert.Equal("READY", ready.Command!.Status);
        Assert.Equal(7, ready.Command.FulfillmentVersion);
        db.ChangeTracker.Clear();
        var fulfillment = await db.FulfillmentOrders.SingleAsync();
        Assert.Equal("READY", fulfillment.FulfillmentStatus);
        Assert.Equal(Now.AddMinutes(2), fulfillment.ReadyAt);
        Assert.Equal(Now.AddMinutes(1), fulfillment.PackedAt);
        var order = await db.SalesOrders.SingleAsync();
        Assert.Equal("READY_FOR_COLLECTION", order.FulfillmentStatus);
        Assert.NotEqual("COLLECTED", order.FulfillmentStatus);
        Assert.Null(order.CompletedAt);
        var pickup = await db.PickupOrders.SingleAsync();
        Assert.Equal("READY", pickup.PickupStatus);
        Assert.Null(pickup.CollectedAt);
        Assert.Single(await db.FulfillmentOrderEvents.ToListAsync(),
            x => x.EventType == PosOnlineOrderPackingRepository.ReadyEvent);
        Assert.Single(await db.PickupOrderEvents.ToListAsync(),
            x => x.EventType == PosOnlineOrderPackingRepository.PickupReadyEvent);
        Assert.Equal(fixture.UserId,
            (await db.FulfillmentOrderEvents.SingleAsync(x =>
                x.EventType == PosOnlineOrderPackingRepository.ReadyEvent)).EventByTenantUserId);
    }

    [Fact]
    public async Task Ready_StaleAndDuplicate_ConflictOnce()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPackableAggregate(db, requested: 1, picked: 1, version: 5);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new PosOnlineOrderPackingRepository(db);

        Assert.True((await repository.PackAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderPackRequest { ExpectedVersion = 5 },
            Now.AddMinutes(1), CancellationToken.None)).IsSuccess);
        db.ChangeTracker.Clear();

        var firstReady = await repository.MarkReadyAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderReadyRequest { ExpectedVersion = 6 },
            Now.AddMinutes(2), CancellationToken.None);
        db.ChangeTracker.Clear();
        var originalReadyAt = (await db.FulfillmentOrders.SingleAsync()).ReadyAt;
        var duplicate = await repository.MarkReadyAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderReadyRequest { ExpectedVersion = 6 },
            Now.AddMinutes(3), CancellationToken.None);
        db.ChangeTracker.Clear();
        var stale = await repository.MarkReadyAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderReadyRequest { ExpectedVersion = 6 },
            Now.AddMinutes(4), CancellationToken.None);

        Assert.True(firstReady.IsSuccess, firstReady.ErrorCode);
        Assert.False(duplicate.IsSuccess);
        Assert.False(stale.IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Equal(originalReadyAt, (await db.FulfillmentOrders.SingleAsync()).ReadyAt);
        Assert.Equal(7, (await db.FulfillmentOrders.SingleAsync()).RowVersion);
        Assert.Single(await db.FulfillmentOrderEvents.ToListAsync(),
            x => x.EventType == PosOnlineOrderPackingRepository.ReadyEvent);
        Assert.Single(await db.PickupOrderEvents.ToListAsync());
        Assert.Equal("READY", (await db.PickupOrders.SingleAsync()).PickupStatus);
    }

    [Fact]
    public async Task FinalPick_DoesNotAutoReady_RequiresPackThenReady()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPackableAggregate(db, requested: 1, picked: 0, version: 3);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var picking = new PosOnlineOrderPickingRepository(db);
        var packing = new PosOnlineOrderPackingRepository(db);

        var pick = await picking.PickLineAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            fixture.FulfillmentLine.Id,
            new PosOnlineOrderPickLineRequest
            {
                Quantity = 1, Barcode = "SKU-1", InputMethod = "SCAN", ExpectedVersion = 3
            }, Now.AddMinutes(1), CancellationToken.None);

        Assert.True(pick.IsSuccess, pick.ErrorCode);
        Assert.True(pick.Command!.CanPack);
        db.ChangeTracker.Clear();
        Assert.Equal("PICKING", (await db.FulfillmentOrders.SingleAsync()).FulfillmentStatus);
        Assert.NotEqual("READY", (await db.SalesOrders.SingleAsync()).FulfillmentStatus);
        Assert.Equal("PENDING", (await db.PickupOrders.SingleAsync()).PickupStatus);

        var readyTooEarly = await packing.MarkReadyAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderReadyRequest { ExpectedVersion = 4 },
            Now.AddMinutes(2), CancellationToken.None);
        Assert.False(readyTooEarly.IsSuccess);
        db.ChangeTracker.Clear();

        var pack = await packing.PackAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderPackRequest { ExpectedVersion = 4 },
            Now.AddMinutes(3), CancellationToken.None);
        Assert.True(pack.IsSuccess, pack.ErrorCode);
        db.ChangeTracker.Clear();
        var refetchPacked = await picking.GetAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            Now.AddMinutes(4), CancellationToken.None);
        Assert.True(refetchPacked.IsSuccess);
        Assert.Equal("PACKED", refetchPacked.Picking!.Status);
        Assert.False(refetchPacked.Picking.CanPack);
        Assert.Equal(5, refetchPacked.Picking.FulfillmentVersion);
        Assert.Equal(1, Assert.Single(refetchPacked.Picking.Lines).PackedQuantity);

        var ready = await packing.MarkReadyAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderReadyRequest { ExpectedVersion = 5 },
            Now.AddMinutes(5), CancellationToken.None);
        Assert.True(ready.IsSuccess, ready.ErrorCode);
        Assert.Equal(6, ready.Command!.FulfillmentVersion);
        db.ChangeTracker.Clear();
        Assert.Equal("READY_FOR_COLLECTION", (await db.SalesOrders.SingleAsync()).FulfillmentStatus);
        Assert.Null((await db.PickupOrders.SingleAsync()).CollectedAt);
    }

    [Fact]
    public async Task Pack_EmptyNoteAccepted_NoDedicatedColumnRequired()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPackableAggregate(db, requested: 1, picked: 1, version: 2);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await new PosOnlineOrderPackingRepository(db).PackAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderPackRequest { ExpectedVersion = 2 },
            Now, CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorCode);
        var packEvent = Assert.Single(await db.FulfillmentOrderEvents.ToListAsync());
        Assert.Equal("Fulfilment packed", packEvent.EventNote);
        Assert.Null((await db.FulfillmentOrders.SingleAsync()).FulfillmentNote);
    }

    internal static PackingFixture SeedPackableAggregate(
        EPosDbContext db,
        decimal requested,
        decimal picked,
        long version,
        decimal cancelled = 0m,
        string status = "PICKING")
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        db.Tenants.Add(Tenant.Create(
            tenantId, "T-1", $"tenant-{tenantId:N}", "Tenant", "active", "LKR",
            "Asia/Colombo", null, null, Now));
        db.TenantUsers.Add(TenantUser.Create(
            userId, tenantId, $"{userId:N}@example.com", "Packer", null, null,
            "hash", "salt", "ACTIVE", "cashier", "cashier", null, Now,
            staffCode: $"STAFF-{userId:N}"));
        db.Outlets.Add(Outlet.Create(
            outletId, tenantId, "Main Store", "MAIN", "ACTIVE", "STORE",
            "Asia/Colombo", true, null, null, userId, Now));

        var order = SalesOrder.CreateClickAndCollect(
            Guid.NewGuid(), tenantId, "EC-PACK-1", "idem-pack-1", Guid.NewGuid(), Guid.NewGuid(),
            "CLICK_COLLECT", outletId, "MAIN", "Main Store", Guid.NewGuid(), "Test Customer",
            "customer@example.com", "+94110000000", "LKR", false, 2000m, 0m, 0m, 0m, 2000m,
            Now.AddHours(2), Now.AddHours(3), "Asia/Colombo", Now);
        var salesLine = SalesOrderLine.CreateForClickAndCollect(
            Guid.NewGuid(), tenantId, order.Id, 1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "SKU-1", "SKU-1", "Product One", "Blue / M", "EA", "Each", "STANDARD", "VARIANT",
            requested, 1000m, requested * 1000m, 0m, 0m, false, Now);
        var methodOutlet = FulfillmentMethodOutlet.Create(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), outletId, null, null, null, "ACTIVE", Now);
        var location = InventoryLocation.Create(
            Guid.NewGuid(), tenantId, outletId, null, "PICK", "Picking Area", "STORAGE",
            false, false, false, false, "ACTIVE", userId, Now);
        var fulfillment = Create<FulfillmentOrder>();
        Set(fulfillment, nameof(fulfillment.Id), Guid.NewGuid());
        Set(fulfillment, nameof(fulfillment.TenantId), tenantId);
        Set(fulfillment, nameof(fulfillment.SalesOrderId), order.Id);
        Set(fulfillment, nameof(fulfillment.FulfillmentNumber), "FUL-PACK-1");
        Set(fulfillment, nameof(fulfillment.FulfillmentMethodOutletId), methodOutlet.Id);
        Set(fulfillment, nameof(fulfillment.SourceInventoryLocationId), location.Id);
        Set(fulfillment, nameof(fulfillment.FulfillmentStatus), status);
        Set(fulfillment, nameof(fulfillment.AssignedToTenantUserId), userId);
        Set(fulfillment, nameof(fulfillment.RowVersion), version);
        Set(fulfillment, nameof(fulfillment.CreatedAt), Now);
        Set(fulfillment, nameof(fulfillment.UpdatedAt), Now);
        var fulfillmentLine = Create<FulfillmentOrderLine>();
        Set(fulfillmentLine, nameof(fulfillmentLine.Id), Guid.NewGuid());
        Set(fulfillmentLine, nameof(fulfillmentLine.TenantId), tenantId);
        Set(fulfillmentLine, nameof(fulfillmentLine.FulfillmentOrderId), fulfillment.Id);
        Set(fulfillmentLine, nameof(fulfillmentLine.SalesOrderLineId), salesLine.Id);
        Set(fulfillmentLine, nameof(fulfillmentLine.RequestedQuantity), requested);
        Set(fulfillmentLine, nameof(fulfillmentLine.PickedQuantity), picked);
        Set(fulfillmentLine, nameof(fulfillmentLine.CancelledQuantity), cancelled);
        Set(fulfillmentLine, nameof(fulfillmentLine.LineStatus),
            picked + cancelled >= requested ? "PICKED" : "PICKING");
        Set(fulfillmentLine, nameof(fulfillmentLine.CreatedAt), Now);
        Set(fulfillmentLine, nameof(fulfillmentLine.UpdatedAt), Now);

        var pickup = Create<PickupOrder>();
        Set(pickup, nameof(pickup.Id), Guid.NewGuid());
        Set(pickup, nameof(pickup.TenantId), tenantId);
        Set(pickup, nameof(pickup.FulfillmentOrderId), fulfillment.Id);
        Set(pickup, nameof(pickup.PickupNumber), "PU-PACK-1");
        Set(pickup, nameof(pickup.PickupContactName), "Test Customer");
        Set(pickup, nameof(pickup.PickupStatus), "PENDING");
        Set(pickup, nameof(pickup.CreatedAt), Now);
        Set(pickup, nameof(pickup.UpdatedAt), Now);

        db.AddRange(order, salesLine, methodOutlet, location, fulfillment, fulfillmentLine, pickup);
        return new(tenantId, userId, outletId, order, fulfillment, fulfillmentLine, pickup);
    }

    private static T Create<T>() where T : class =>
        (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

    private static void Set<T>(T target, string property, object? value) where T : class =>
        typeof(T).GetProperty(property)!.SetValue(target, value);

    private static EPosDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    internal sealed record PackingFixture(
        Guid TenantId, Guid UserId, Guid OutletId,
        SalesOrder Order, FulfillmentOrder Fulfillment,
        FulfillmentOrderLine FulfillmentLine, PickupOrder Pickup);
}
