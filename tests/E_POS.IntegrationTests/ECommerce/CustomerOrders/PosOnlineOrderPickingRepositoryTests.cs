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

public sealed class PosOnlineOrderPickingRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAsync_PickingOrder_ReturnsProjectionVersionLocationAndPackEligibility()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPickingAggregate(db, requested: 2, picked: 1, version: 5);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new PosOnlineOrderPickingRepository(db);

        var result = await repository.GetAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            Now.AddMinutes(1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var response = result.Picking!;
        Assert.Equal(5, response.FulfillmentVersion);
        Assert.False(response.CanPack);
        Assert.Equal(1, response.PickedUnits);
        Assert.Equal(1, response.RemainingUnits);
        var line = Assert.Single(response.Lines);
        Assert.Equal("SKU-1", line.Barcode);
        Assert.Equal("PICK", line.LocationCode);
        Assert.Equal("Picking Area", line.LocationName);
    }

    [Fact]
    public async Task PickLine_SuccessThenStaleRequest_FirstMutatesExactlyOnceSecondDoesNothing()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPickingAggregate(db, requested: 2, picked: 0, version: 5);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new PosOnlineOrderPickingRepository(db);
        var request = new PosOnlineOrderPickLineRequest
        {
            Quantity = 1, Barcode = "SKU-1", InputMethod = "SCAN", ExpectedVersion = 5
        };

        var first = await repository.PickLineAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            fixture.FulfillmentLine.Id, request, Now.AddMinutes(1), CancellationToken.None);
        db.ChangeTracker.Clear();
        var stale = await repository.PickLineAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            fixture.FulfillmentLine.Id, request, Now.AddMinutes(2), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.Equal(6, first.Command!.FulfillmentVersion);
        Assert.False(stale.IsSuccess);
        Assert.Equal("online_orders.concurrency_conflict", stale.ErrorCode);
        db.ChangeTracker.Clear();
        Assert.Equal(1, (await db.FulfillmentOrderLines.SingleAsync()).PickedQuantity);
        Assert.Equal(6, (await db.FulfillmentOrders.SingleAsync()).RowVersion);
        var savedEvent = Assert.Single(await db.FulfillmentOrderEvents.ToListAsync());
        Assert.Equal(PosOnlineOrderPickingRepository.LinePickedEvent, savedEvent.EventType);
    }

    [Fact]
    public async Task PickLine_InvalidBarcode_LeavesQuantityVersionAndEventsUnchanged()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPickingAggregate(db, requested: 2, picked: 0, version: 5);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new PosOnlineOrderPickingRepository(db);

        var result = await repository.PickLineAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            fixture.FulfillmentLine.Id,
            new PosOnlineOrderPickLineRequest
            {
                Quantity = 1, Barcode = "OTHER", InputMethod = "SCAN", ExpectedVersion = 5
            }, Now.AddMinutes(1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("online_orders.invalid_barcode", result.ErrorCode);
        db.ChangeTracker.Clear();
        Assert.Equal(0, (await db.FulfillmentOrderLines.SingleAsync()).PickedQuantity);
        Assert.Equal(5, (await db.FulfillmentOrders.SingleAsync()).RowVersion);
        Assert.Empty(await db.FulfillmentOrderEvents.ToListAsync());
    }

    [Fact]
    public async Task PickLine_FinalRequiredUnit_ReturnsCanPackAndAppendsCompletionEventOnce()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPickingAggregate(db, requested: 1, picked: 0, version: 3);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new PosOnlineOrderPickingRepository(db);

        var result = await repository.PickLineAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            fixture.FulfillmentLine.Id,
            new PosOnlineOrderPickLineRequest
            {
                Quantity = 1, Barcode = "SKU-1", InputMethod = "SCAN", ExpectedVersion = 3
            }, Now.AddMinutes(1), CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorCode);
        Assert.True(result.Command!.CanPack);
        Assert.Equal(4, result.Command.FulfillmentVersion);
        var events = await db.FulfillmentOrderEvents.OrderBy(x => x.SequenceNumber).ToListAsync();
        Assert.Collection(events,
            item => Assert.Equal(PosOnlineOrderPickingRepository.LinePickedEvent, item.EventType),
            item => Assert.Equal(PosOnlineOrderPickingRepository.PickingCompletedEvent, item.EventType));
    }

    [Fact]
    public async Task ReportIssue_AppendsAuditAndVersionButDoesNotChangeQuantityOrPackEligibility()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPickingAggregate(db, requested: 2, picked: 0, version: 5);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new PosOnlineOrderPickingRepository(db);

        var result = await repository.ReportIssueAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            fixture.FulfillmentLine.Id,
            new PosOnlineOrderPickingIssueRequest
            {
                Reason = "ITEM_NOT_FOUND", Note = "Shelf checked", ExpectedVersion = 5
            }, Now.AddMinutes(1), CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorCode);
        Assert.Equal(6, result.Command!.FulfillmentVersion);
        Assert.False(result.Command.CanPack);
        db.ChangeTracker.Clear();
        Assert.Equal(0, (await db.FulfillmentOrderLines.SingleAsync()).PickedQuantity);
        Assert.Equal(6, (await db.FulfillmentOrders.SingleAsync()).RowVersion);
        var savedEvent = Assert.Single(await db.FulfillmentOrderEvents.ToListAsync());
        Assert.Equal(PosOnlineOrderPickingRepository.IssueReportedEvent, savedEvent.EventType);
        Assert.Contains(fixture.FulfillmentLine.Id.ToString("D"), savedEvent.EventPayloadJson);
    }

    [Fact]
    public async Task AddNote_SuccessThenStale_PersistsOnceAndGetReturnsAuthoritativeNote()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPickingAggregate(db, requested: 2, picked: 0, version: 5);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new PosOnlineOrderPickingRepository(db);
        var request = new PosOnlineOrderPickingNoteRequest
        {
            Note = "Checked upper shelf", ExpectedVersion = 5
        };

        var first = await repository.AddNoteAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            request, Now.AddMinutes(1), CancellationToken.None);
        db.ChangeTracker.Clear();
        var stale = await repository.AddNoteAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            request, Now.AddMinutes(2), CancellationToken.None);
        db.ChangeTracker.Clear();
        var second = await repository.AddNoteAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            new PosOnlineOrderPickingNoteRequest
            {
                Note = "Checked stock room", ExpectedVersion = 6
            }, Now.AddMinutes(3), CancellationToken.None);
        db.ChangeTracker.Clear();
        var detail = await repository.GetAsync(
            fixture.TenantId, fixture.UserId, fixture.OutletId, fixture.Order.Id,
            Now.AddMinutes(4), CancellationToken.None);

        Assert.True(first.IsSuccess, first.ErrorCode);
        Assert.Equal(6, first.NoteCommand!.FulfillmentVersion);
        Assert.Equal("Checked upper shelf", first.NoteCommand.Note.Note);
        Assert.Equal("Picker", first.NoteCommand.Note.CreatedByDisplayName);
        Assert.False(stale.IsSuccess);
        Assert.Equal("online_orders.concurrency_conflict", stale.ErrorCode);
        Assert.True(second.IsSuccess, second.ErrorCode);
        Assert.Equal(7, (await db.FulfillmentOrders.SingleAsync()).RowVersion);
        Assert.Equal(0, (await db.FulfillmentOrderLines.SingleAsync()).PickedQuantity);
        var savedEvents = await db.FulfillmentOrderEvents.OrderBy(x => x.SequenceNumber).ToListAsync();
        Assert.Equal(2, savedEvents.Count);
        Assert.All(savedEvents, savedEvent =>
        {
            Assert.Equal(PosOnlineOrderPickingRepository.PickingNoteAddedEvent, savedEvent.EventType);
            Assert.Equal(fixture.UserId, savedEvent.EventByTenantUserId);
        });
        Assert.Equal("Checked upper shelf", savedEvents[0].EventNote);
        Assert.Equal(Now.AddMinutes(1), savedEvents[0].EventAt);
        Assert.Equal("Checked stock room", savedEvents[1].EventNote);
        Assert.Collection(detail.Picking!.Notes,
            returnedNote => Assert.Equal("Checked upper shelf", returnedNote.Note),
            returnedNote => Assert.Equal("Checked stock room", returnedNote.Note));
        Assert.False(detail.Picking.CanPack);
    }

    [Fact]
    public async Task AddNote_WrongTenantOrOutlet_DoesNotMutateAggregateOrEvents()
    {
        await using var db = CreateDbContext();
        var fixture = SeedPickingAggregate(db, requested: 2, picked: 0, version: 5);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new PosOnlineOrderPickingRepository(db);
        var request = new PosOnlineOrderPickingNoteRequest { Note = "No access", ExpectedVersion = 5 };

        var wrongTenant = await repository.AddNoteAsync(
            Guid.NewGuid(), fixture.UserId, fixture.OutletId, fixture.Order.Id,
            request, Now.AddMinutes(1), CancellationToken.None);
        var wrongOutlet = await repository.AddNoteAsync(
            fixture.TenantId, fixture.UserId, Guid.NewGuid(), fixture.Order.Id,
            request, Now.AddMinutes(1), CancellationToken.None);

        Assert.False(wrongTenant.IsSuccess);
        Assert.False(wrongOutlet.IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Equal(5, (await db.FulfillmentOrders.SingleAsync()).RowVersion);
        Assert.Equal(0, (await db.FulfillmentOrderLines.SingleAsync()).PickedQuantity);
        Assert.Empty(await db.FulfillmentOrderEvents.ToListAsync());
    }

    private static PickingFixture SeedPickingAggregate(
        EPosDbContext db, decimal requested, decimal picked, long version)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        db.Tenants.Add(Tenant.Create(
            tenantId, "T-1", $"tenant-{tenantId:N}", "Tenant", "active", "LKR",
            "Asia/Colombo", null, null, Now));
        db.TenantUsers.Add(TenantUser.Create(
            userId, tenantId, $"{userId:N}@example.com", "Picker", null, null,
            "hash", "salt", "ACTIVE", "cashier", "cashier", null, Now,
            staffCode: $"STAFF-{userId:N}"));
        db.Outlets.Add(Outlet.Create(
            outletId, tenantId, "Main Store", "MAIN", "ACTIVE", "STORE",
            "Asia/Colombo", true, null, null, userId, Now));

        var order = SalesOrder.CreateClickAndCollect(
            Guid.NewGuid(), tenantId, "EC-PICK-1", "idem-pick-1", Guid.NewGuid(), Guid.NewGuid(),
            "CLICK_COLLECT", outletId, "MAIN", "Main Store", Guid.NewGuid(), "Test Customer",
            "customer@example.com", "+94110000000", "LKR", false, 2000m, 0m, 0m, 0m, 2000m,
            Now.AddHours(2), Now.AddHours(3), "Asia/Colombo", Now);
        var salesLine = SalesOrderLine.CreateForClickAndCollect(
            Guid.NewGuid(), tenantId, order.Id, 1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "SKU-1", "Product One", "Blue / M", "EA", "Each", "STANDARD", "VARIANT",
            requested, 1000m, requested * 1000m, 0m, 0m, false, Now);
        Set(salesLine, nameof(salesLine.BarcodeSnapshot), "SKU-1");
        var methodOutlet = FulfillmentMethodOutlet.Create(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), outletId, null, null, null, "ACTIVE", Now);
        var location = InventoryLocation.Create(
            Guid.NewGuid(), tenantId, outletId, null, "PICK", "Picking Area", "STORAGE",
            false, false, false, false, "ACTIVE", userId, Now);
        var fulfillment = Create<FulfillmentOrder>();
        Set(fulfillment, nameof(fulfillment.Id), Guid.NewGuid());
        Set(fulfillment, nameof(fulfillment.TenantId), tenantId);
        Set(fulfillment, nameof(fulfillment.SalesOrderId), order.Id);
        Set(fulfillment, nameof(fulfillment.FulfillmentNumber), "FUL-PICK-1");
        Set(fulfillment, nameof(fulfillment.FulfillmentMethodOutletId), methodOutlet.Id);
        Set(fulfillment, nameof(fulfillment.SourceInventoryLocationId), location.Id);
        Set(fulfillment, nameof(fulfillment.FulfillmentStatus), "PICKING");
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
        Set(fulfillmentLine, nameof(fulfillmentLine.LineStatus), picked == requested ? "PICKED" : "PICKING");
        Set(fulfillmentLine, nameof(fulfillmentLine.CreatedAt), Now);
        Set(fulfillmentLine, nameof(fulfillmentLine.UpdatedAt), Now);

        db.AddRange(order, salesLine, methodOutlet, location, fulfillment, fulfillmentLine);
        return new(tenantId, userId, outletId, order, fulfillmentLine);
    }

    private static T Create<T>() where T : class =>
        (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

    private static void Set<T>(T target, string property, object? value) where T : class =>
        typeof(T).GetProperty(property)!.SetValue(target, value);

    private static EPosDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed record PickingFixture(
        Guid TenantId, Guid UserId, Guid OutletId,
        SalesOrder Order, FulfillmentOrderLine FulfillmentLine);
}
