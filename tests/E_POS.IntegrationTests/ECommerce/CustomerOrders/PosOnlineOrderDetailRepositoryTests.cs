using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.ECommerce.CustomerOrders;

public sealed class PosOnlineOrderDetailRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 5, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Model_FulfillmentOrderRowVersion_IsConcurrencyToken()
    {
        using var db = CreateDbContext();

        var property = db.Model.FindEntityType(typeof(FulfillmentOrder))!
            .FindProperty(nameof(FulfillmentOrder.RowVersion))!;

        Assert.True(property.IsConcurrencyToken);
        Assert.Equal("row_version", property.GetColumnName());
    }

    [Fact]
    public async Task GetAsync_AuthorizedOutlet_ReturnsAuthoritativeDetailWithoutMutation()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedAccessContext(db, tenantId, userId, outletId);
        var order = CreateOrder(tenantId, outletId);
        var line = CreateLine(tenantId, order.Id);
        db.SalesOrders.Add(order);
        db.SalesOrderLines.Add(line);
        await db.SaveChangesAsync();
        var originalUpdatedAt = order.UpdatedAt;
        var repository = new PosOnlineOrderDetailRepository(db);

        var result = await repository.GetAsync(
            tenantId, userId, outletId, order.Id, Now.AddMinutes(3), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var detail = result.Detail!;
        Assert.Equal(order.OrderNumber, detail.OrderNumber);
        Assert.Equal("PENDING_CONFIRMATION", detail.Status);
        Assert.Equal("Test Customer", detail.CustomerName);
        Assert.Null(detail.CustomerClassification);
        Assert.Equal("UNPAID", detail.PaymentStatus);
        Assert.Equal(1, detail.ItemCount);
        Assert.Equal(2m, detail.UnitCount);
        Assert.Equal(2m, Assert.Single(detail.Lines).RemainingQuantity);
        Assert.Equal(Now.AddMinutes(3), detail.ServerTime);

        db.ChangeTracker.Clear();
        var persisted = await db.SalesOrders.AsNoTracking().SingleAsync(x => x.Id == order.Id);
        Assert.Equal(originalUpdatedAt, persisted.UpdatedAt);
        Assert.Empty(await db.FulfillmentOrderEvents.ToListAsync());
        Assert.Empty(await db.SalesOrderStatusHistory.ToListAsync());
    }

    [Fact]
    public async Task GetAsync_OrderAtDifferentOutlet_IsNonDisclosingNotFound()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var requestedOutletId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedAccessContext(db, tenantId, userId, requestedOutletId);
        var order = CreateOrder(tenantId, Guid.NewGuid());
        db.SalesOrders.Add(order);
        await db.SaveChangesAsync();
        var repository = new PosOnlineOrderDetailRepository(db);

        var result = await repository.GetAsync(
            tenantId, userId, requestedOutletId, order.Id, Now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.not_found", result.ErrorCode);
    }

    [Fact]
    public async Task GetAsync_InactiveOrUnknownAccessContext_IsDeniedBeforeOrderRead()
    {
        await using var db = CreateDbContext();
        var repository = new PosOnlineOrderDetailRepository(db);

        var result = await repository.GetAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.outlet_access_denied", result.ErrorCode);
    }

    [Fact]
    public async Task StartAsync_SameVersionTwice_OnlyFirstTransitionsAssignsAndAppendsEvent()
    {
        var tenantId = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedAccessContext(db, tenantId, userA, outletId);
        db.TenantUsers.Add(TenantUser.Create(
            userB, tenantId, $"{userB:N}@example.com", "Cashier B", null, null, "hash", "salt",
            "ACTIVE", "cashier", "cashier", null, Now, staffCode: $"STAFF-{userB:N}"));
        var order = CreateOrder(tenantId, outletId);
        var fulfillmentMethodOutlet = FulfillmentMethodOutlet.Create(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), outletId, null, null, null, "ACTIVE", Now);
        var fulfillment = CreateFulfillment(tenantId, order.Id, fulfillmentMethodOutlet.Id, "PENDING", 5);
        var pickupSlot = CreatePickupSlot(tenantId, fulfillmentMethodOutlet.Id);
        var pickupReservation = PickupSlotReservation.CreatePending(
            Guid.NewGuid(), tenantId, pickupSlot.Id, Guid.NewGuid(), 1, Now.AddHours(1), Now);
        pickupReservation.Confirm(order.Id, Now);
        var pickup = CreatePickup(tenantId, fulfillment.Id, pickupReservation.Id);
        var inventoryReservation = InventoryReservation.Create(
            Guid.NewGuid(), tenantId, "RES-1", "SALES_ORDER", order.Id, order.OrderNumber,
            order.SalesChannelId, outletId, order.CustomerId, "CONFIRMED", Now, Now.AddHours(1), userA, Now);
        db.AddRange(order, fulfillmentMethodOutlet, fulfillment, pickupSlot, pickupReservation, pickup, inventoryReservation);
        await db.SaveChangesAsync();
        var detailRepository = new PosOnlineOrderDetailRepository(db);
        var repository = new PosOnlineOrderStartFulfillmentRepository(db);

        var detail = await detailRepository.GetAsync(
            tenantId, userA, outletId, order.Id, Now, CancellationToken.None);
        var first = await repository.StartAsync(tenantId, userA, outletId, order.Id, 5, Now.AddMinutes(1), CancellationToken.None);
        var second = await repository.StartAsync(tenantId, userB, outletId, order.Id, 5, Now.AddMinutes(2), CancellationToken.None);

        Assert.True(detail.IsSuccess);
        Assert.Equal(5, detail.Detail!.FulfillmentVersion);
        Assert.True(first.IsSuccess);
        Assert.Equal(6, first.Response!.FulfillmentVersion);
        Assert.False(second.IsSuccess);
        Assert.Equal("online_orders.concurrency_conflict", second.ErrorCode);
        db.ChangeTracker.Clear();
        var persisted = await db.FulfillmentOrders.SingleAsync(x => x.Id == fulfillment.Id);
        Assert.Equal("PICKING", persisted.FulfillmentStatus);
        Assert.Equal(userA, persisted.AssignedToTenantUserId);
        Assert.Equal(6, persisted.RowVersion);
        var savedEvent = Assert.Single(await db.FulfillmentOrderEvents.ToListAsync());
        Assert.Equal("FULFILLMENT_STARTED", savedEvent.EventType);
        Assert.Equal(userA, savedEvent.EventByTenantUserId);
    }

    [Fact]
    public async Task StartAsync_PickupSlotAtDifferentOutlet_DoesNotMutateOrAppendEvent()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var requestedOutletId = Guid.NewGuid();
        var otherOutletId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedAccessContext(db, tenantId, userId, requestedOutletId);
        db.Outlets.Add(Outlet.Create(
            otherOutletId, tenantId, "Other Store", "OTHER", "ACTIVE", "STORE",
            "Asia/Colombo", true, null, null, userId, Now));
        var order = CreateOrder(tenantId, requestedOutletId);
        var orderMethodOutlet = FulfillmentMethodOutlet.Create(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), requestedOutletId,
            null, null, null, "ACTIVE", Now);
        var otherMethodOutlet = FulfillmentMethodOutlet.Create(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), otherOutletId,
            null, null, null, "ACTIVE", Now);
        var fulfillment = CreateFulfillment(
            tenantId, order.Id, orderMethodOutlet.Id, "PENDING", 5);
        var wrongOutletSlot = CreatePickupSlot(tenantId, otherMethodOutlet.Id);
        var pickupReservation = PickupSlotReservation.CreatePending(
            Guid.NewGuid(), tenantId, wrongOutletSlot.Id, Guid.NewGuid(), 1,
            Now.AddHours(1), Now);
        pickupReservation.Confirm(order.Id, Now);
        var pickup = CreatePickup(tenantId, fulfillment.Id, pickupReservation.Id);
        var inventoryReservation = InventoryReservation.Create(
            Guid.NewGuid(), tenantId, "RES-WRONG-SLOT", "SALES_ORDER", order.Id,
            order.OrderNumber, order.SalesChannelId, requestedOutletId,
            order.CustomerId, "CONFIRMED", Now, Now.AddHours(1), userId, Now);
        db.AddRange(
            order, orderMethodOutlet, otherMethodOutlet, fulfillment,
            wrongOutletSlot, pickupReservation, pickup, inventoryReservation);
        await db.SaveChangesAsync();
        var repository = new PosOnlineOrderStartFulfillmentRepository(db);

        var result = await repository.StartAsync(
            tenantId, userId, requestedOutletId, order.Id, 5, Now.AddMinutes(1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.invalid_reservation", result.ErrorCode);
        db.ChangeTracker.Clear();
        var persisted = await db.FulfillmentOrders
            .SingleAsync(x => x.Id == fulfillment.Id);
        Assert.Equal("PENDING", persisted.FulfillmentStatus);
        Assert.Null(persisted.AssignedToTenantUserId);
        Assert.Equal(5, persisted.RowVersion);
        Assert.Empty(await db.FulfillmentOrderEvents.ToListAsync());
    }

    [Theory]
    [InlineData("UNCONFIRMED")]
    [InlineData("WRONG_ORDER")]
    [InlineData("WRONG_TENANT")]
    public async Task StartAsync_InvalidPickupReservationOwnership_DoesNotMutateOrAppendEvent(
        string invalidCase)
    {
        var tenantId = Guid.NewGuid();
        var reservationTenantId = invalidCase == "WRONG_TENANT" ? Guid.NewGuid() : tenantId;
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedAccessContext(db, tenantId, userId, outletId);
        var order = CreateOrder(tenantId, outletId);
        var methodOutlet = FulfillmentMethodOutlet.Create(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), outletId,
            null, null, null, "ACTIVE", Now);
        var reservationMethodOutlet = reservationTenantId == tenantId
            ? methodOutlet
            : FulfillmentMethodOutlet.Create(
                Guid.NewGuid(), reservationTenantId, Guid.NewGuid(), Guid.NewGuid(),
                null, null, null, "ACTIVE", Now);
        var fulfillment = CreateFulfillment(
            tenantId, order.Id, methodOutlet.Id, "PENDING", 5);
        var pickupSlot = CreatePickupSlot(reservationTenantId, reservationMethodOutlet.Id);
        var pickupReservation = PickupSlotReservation.CreatePending(
            Guid.NewGuid(), reservationTenantId, pickupSlot.Id, Guid.NewGuid(), 1,
            Now.AddHours(1), Now);
        if (invalidCase != "UNCONFIRMED")
        {
            pickupReservation.Confirm(
                invalidCase == "WRONG_ORDER" ? Guid.NewGuid() : order.Id, Now);
        }
        var pickup = CreatePickup(tenantId, fulfillment.Id, pickupReservation.Id);
        var inventoryReservation = InventoryReservation.Create(
            Guid.NewGuid(), tenantId, $"RES-{invalidCase}", "SALES_ORDER", order.Id,
            order.OrderNumber, order.SalesChannelId, outletId, order.CustomerId,
            "CONFIRMED", Now, Now.AddHours(1), userId, Now);
        db.AddRange(order, methodOutlet, fulfillment, pickupSlot, pickupReservation,
            pickup, inventoryReservation);
        if (reservationMethodOutlet != methodOutlet)
            db.Add(reservationMethodOutlet);
        await db.SaveChangesAsync();
        var repository = new PosOnlineOrderStartFulfillmentRepository(db);

        var result = await repository.StartAsync(
            tenantId, userId, outletId, order.Id, 5, Now.AddMinutes(1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.invalid_reservation", result.ErrorCode);
        db.ChangeTracker.Clear();
        var persisted = await db.FulfillmentOrders.SingleAsync(x => x.Id == fulfillment.Id);
        Assert.Equal("PENDING", persisted.FulfillmentStatus);
        Assert.Null(persisted.AssignedToTenantUserId);
        Assert.Equal(5, persisted.RowVersion);
        Assert.Empty(await db.FulfillmentOrderEvents.ToListAsync());
    }

    [Fact]
    public async Task StartAsync_ExpiredInventoryReservation_DoesNotMutateOrAppendEvent()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedAccessContext(db, tenantId, userId, outletId);
        var order = CreateOrder(tenantId, outletId);
        var methodOutlet = FulfillmentMethodOutlet.Create(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), outletId,
            null, null, null, "ACTIVE", Now);
        var fulfillment = CreateFulfillment(
            tenantId, order.Id, methodOutlet.Id, "ALLOCATED", 5);
        var pickupSlot = CreatePickupSlot(tenantId, methodOutlet.Id);
        var pickupReservation = PickupSlotReservation.CreatePending(
            Guid.NewGuid(), tenantId, pickupSlot.Id, Guid.NewGuid(), 1,
            Now.AddHours(1), Now);
        pickupReservation.Confirm(order.Id, Now);
        var pickup = CreatePickup(tenantId, fulfillment.Id, pickupReservation.Id);
        var inventoryReservation = InventoryReservation.Create(
            Guid.NewGuid(), tenantId, "RES-EXPIRED", "SALES_ORDER", order.Id,
            order.OrderNumber, order.SalesChannelId, outletId, order.CustomerId,
            "CONFIRMED", Now.AddHours(-2), Now.AddMinutes(-1), userId, Now.AddHours(-2));
        db.AddRange(order, methodOutlet, fulfillment, pickupSlot, pickupReservation,
            pickup, inventoryReservation);
        await db.SaveChangesAsync();
        var repository = new PosOnlineOrderStartFulfillmentRepository(db);

        var result = await repository.StartAsync(
            tenantId, userId, outletId, order.Id, 5, Now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.invalid_reservation", result.ErrorCode);
        db.ChangeTracker.Clear();
        var persisted = await db.FulfillmentOrders.SingleAsync(x => x.Id == fulfillment.Id);
        Assert.Equal("ALLOCATED", persisted.FulfillmentStatus);
        Assert.Null(persisted.AssignedToTenantUserId);
        Assert.Equal(5, persisted.RowVersion);
        Assert.Empty(await db.FulfillmentOrderEvents.ToListAsync());
    }

    [Fact]
    public async Task StartAsync_MissingReservations_DoesNotMutateOrAppendEvent()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        await using var db = CreateDbContext();
        SeedAccessContext(db, tenantId, userId, outletId);
        var order = CreateOrder(tenantId, outletId);
        var methodOutlet = FulfillmentMethodOutlet.Create(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), outletId, null, null, null, "ACTIVE", Now);
        var fulfillment = CreateFulfillment(tenantId, order.Id, methodOutlet.Id, "PENDING", 1);
        db.AddRange(order, methodOutlet, fulfillment);
        await db.SaveChangesAsync();
        var repository = new PosOnlineOrderStartFulfillmentRepository(db);

        var result = await repository.StartAsync(tenantId, userId, outletId, order.Id, 1, Now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.invalid_reservation", result.ErrorCode);
        Assert.Equal("PENDING", fulfillment.FulfillmentStatus);
        Assert.Equal(1, fulfillment.RowVersion);
        Assert.Empty(await db.FulfillmentOrderEvents.ToListAsync());
    }

    private static void SeedAccessContext(EPosDbContext db, Guid tenantId, Guid userId, Guid outletId)
    {
        db.Tenants.Add(Tenant.Create(
            tenantId, "T-1", $"tenant-{tenantId:N}", "Tenant", "active", "LKR", "Asia/Colombo", null, null, Now));
        db.TenantUsers.Add(TenantUser.Create(
            userId, tenantId, $"{userId:N}@example.com", "Cashier", null, null, "hash", "salt", "ACTIVE", "cashier", "cashier", null, Now,
            staffCode: $"STAFF-{userId:N}"));
        db.Outlets.Add(Outlet.Create(
            outletId, tenantId, "Main Store", "MAIN", "ACTIVE", "STORE", "Asia/Colombo", true, null, null, userId, Now));
    }

    private static SalesOrder CreateOrder(Guid tenantId, Guid outletId) =>
        SalesOrder.CreateClickAndCollect(
            Guid.NewGuid(), tenantId, "EC-1001", "idem-ec-1001", Guid.NewGuid(), Guid.NewGuid(),
            "CLICK_COLLECT", outletId, "MAIN", "Main Store", Guid.NewGuid(), "Test Customer",
            "customer@example.com", "+94110000000", "LKR", false, 2000m, 0m, 0m, 0m, 2000m,
            Now.AddHours(2), Now.AddHours(3), "Asia/Colombo", Now);

    private static SalesOrderLine CreateLine(Guid tenantId, Guid orderId) =>
        SalesOrderLine.CreateForClickAndCollect(
            Guid.NewGuid(), tenantId, orderId, 1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "SKU-1", "Product One", "Blue / M", "EA", "Each", "STANDARD", "VARIANT",
            2m, 1000m, 2000m, 0m, 0m, false, Now);

    private static FulfillmentOrder CreateFulfillment(
        Guid tenantId, Guid orderId, Guid methodOutletId, string status, long version)
    {
        var entity = (FulfillmentOrder)Activator.CreateInstance(typeof(FulfillmentOrder), nonPublic: true)!;
        Set(entity, nameof(entity.Id), Guid.NewGuid());
        Set(entity, nameof(entity.TenantId), tenantId);
        Set(entity, nameof(entity.SalesOrderId), orderId);
        Set(entity, nameof(entity.FulfillmentNumber), $"FUL-{orderId:N}");
        Set(entity, nameof(entity.FulfillmentMethodOutletId), methodOutletId);
        Set(entity, nameof(entity.FulfillmentStatus), status);
        Set(entity, nameof(entity.RowVersion), version);
        Set(entity, nameof(entity.CreatedAt), Now);
        Set(entity, nameof(entity.UpdatedAt), Now);
        return entity;
    }

    private static PickupOrder CreatePickup(Guid tenantId, Guid fulfillmentId, Guid reservationId)
    {
        var entity = (PickupOrder)Activator.CreateInstance(typeof(PickupOrder), nonPublic: true)!;
        Set(entity, nameof(entity.Id), Guid.NewGuid());
        Set(entity, nameof(entity.TenantId), tenantId);
        Set(entity, nameof(entity.FulfillmentOrderId), fulfillmentId);
        Set(entity, nameof(entity.PickupSlotReservationId), reservationId);
        Set(entity, nameof(entity.PickupNumber), $"PICK-{fulfillmentId:N}");
        Set(entity, nameof(entity.PickupContactName), "Test Customer");
        Set(entity, nameof(entity.PickupStatus), "PENDING");
        Set(entity, nameof(entity.CreatedAt), Now);
        Set(entity, nameof(entity.UpdatedAt), Now);
        return entity;
    }

    private static PickupSlot CreatePickupSlot(
        Guid tenantId,
        Guid fulfillmentMethodOutletId)
    {
        var entity = (PickupSlot)Activator.CreateInstance(
            typeof(PickupSlot), nonPublic: true)!;
        Set(entity, nameof(entity.Id), Guid.NewGuid());
        Set(entity, nameof(entity.TenantId), tenantId);
        Set(entity, nameof(entity.FulfillmentMethodOutletId), fulfillmentMethodOutletId);
        Set(entity, nameof(entity.SlotCode), $"SLOT-{Guid.NewGuid():N}");
        Set(entity, nameof(entity.SlotDate), DateOnly.FromDateTime(Now.UtcDateTime));
        Set(entity, nameof(entity.WindowStart), new TimeOnly(10, 0));
        Set(entity, nameof(entity.WindowEnd), new TimeOnly(11, 0));
        Set(entity, nameof(entity.Capacity), 10);
        Set(entity, nameof(entity.ReservedCount), 1);
        Set(entity, nameof(entity.SlotStatus), "OPEN");
        Set(entity, nameof(entity.RowVersion), 1L);
        Set(entity, nameof(entity.CreatedAt), Now);
        Set(entity, nameof(entity.UpdatedAt), Now);
        return entity;
    }

    private static void Set<T>(T target, string property, object? value) where T : class =>
        typeof(T).GetProperty(property)!.SetValue(target, value);

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}
