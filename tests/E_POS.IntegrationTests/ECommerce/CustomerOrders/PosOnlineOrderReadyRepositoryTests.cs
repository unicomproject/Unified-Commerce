using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Services;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Shared.Notification.Channels;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using E_POS.Application.Modules.Shared.Notification.Dtos;
using E_POS.Application.Modules.Shared.Notification.Services;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Constants;
using E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;
using E_POS.Infrastructure.Modules.Shared.Notification.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static E_POS.IntegrationTests.ECommerce.CustomerOrders.PosOnlineOrderPackingRepositoryTests;

namespace E_POS.IntegrationTests.ECommerce.CustomerOrders;

public sealed class PosOnlineOrderReadyRepositoryTests
{
    internal static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-09T08:00:00Z");

    [Theory]
    [InlineData("PICKING")] [InlineData("PACKED")] [InlineData("READY")]
    public async Task Get_AcceptsSupportedStates(string state)
    {
        await using var db = CreateDb();
        var f = Seed(db, state);
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var result = await Get(db, f);
        Assert.True(result.IsSuccess, result.ErrorCode);
        Assert.Equal(state, result.Picking!.FulfillmentStatus);
        Assert.Equal(Now, result.Picking.ServerTime);
        Assert.Equal(f.Order.RequestedCollectionAt, result.Picking.CollectionAt);
        Assert.Equal(f.Order.RequestedCollectionEndAt, result.Picking.CollectionEndAt);
        Assert.Equal(2, result.Picking.TotalUnits); // 3 requested minus 1 cancelled
        Assert.Equal(2, result.Picking.PickedUnits);
        Assert.Equal(0, result.Picking.PendingUnits);
        if (state == "READY")
        {
            Assert.Equal("READY", result.Picking.PickupStatus);
            Assert.Equal(Now, result.Picking.ReadyAt);
            Assert.Null(result.Picking.CollectedAt);
            Assert.False(result.Picking.CanPack);
        }
    }

    [Theory]
    [InlineData("FulfillmentStatus", "CANCELLED")]
    [InlineData("FulfillmentStatus", "FULFILLED")]
    [InlineData("PickupStatus", "COLLECTED")]
    [InlineData("PickupStatus", "CANCELLED")]
    [InlineData("PickupStatus", "VERIFIED")]
    [InlineData("Status", "COMPLETED")]
    [InlineData("Status", "CANCELLED")]
    public async Task GetAndNotify_RejectTerminalOrInconsistentState(string property, string state)
    {
        await using var db = CreateDb();
        var f = Seed(db);
        Set(property == "Status" ? f.Order : property == "PickupStatus" ? f.Pickup : f.Fulfillment, property, state);
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.False((await Get(db, f)).IsSuccess);
        var notify = await Service(db).NotifyAsync(Context(f), f.OutletId, f.Order.Id, default);
        Assert.Equal("online_orders.invalid_state", notify.Error.Code);
        Assert.Empty(await db.NotificationEvents.ToListAsync());
    }

    [Theory]
    [InlineData("PICKING")] [InlineData("PACKED")]
    public async Task Notify_RejectsPreReady(string state)
    {
        await using var db = CreateDb();
        var f = Seed(db, state);
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal("online_orders.invalid_state",
            (await Service(db).NotifyAsync(Context(f), f.OutletId, f.Order.Id, default)).Error.Code);
        Assert.Empty(await db.NotificationEvents.ToListAsync());
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task GetAndNotify_RejectMissingReadyAtOrCollectedAt(bool missingReadyAt)
    {
        await using var db = CreateDb();
        var f = Seed(db);
        if (missingReadyAt) Set(f.Fulfillment, "ReadyAt", null);
        else Set(f.Pickup, "CollectedAt", Now);
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.False((await Get(db, f)).IsSuccess);
        Assert.False((await Service(db).NotifyAsync(Context(f), f.OutletId, f.Order.Id, default)).IsSuccess);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task GetAndNotify_RejectWrongScope(bool wrongTenant)
    {
        await using var db = CreateDb();
        var f = Seed(db);
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var tenant = wrongTenant ? Guid.NewGuid() : f.TenantId;
        var outlet = wrongTenant ? f.OutletId : Guid.NewGuid();
        Assert.False((await new PosOnlineOrderPickingRepository(db).GetAsync(
            tenant, f.UserId, outlet, f.Order.Id, Now, default)).IsSuccess);
        var context = new TenantRequestContext(tenant, f.UserId, Permissions);
        Assert.False((await Service(db).NotifyAsync(context, outlet, f.Order.Id, default)).IsSuccess);
        Assert.Empty(await db.NotificationEvents.ToListAsync());
    }

    [Theory]
    [InlineData("missing")] [InlineData("other-tenant")] [InlineData("inactive")]
    public async Task Notify_RejectsInvalidRecipient(string kind)
    {
        await using var db = CreateDb();
        var f = Seed(db);
        var customer = db.Customers.Local.Single();
        if (kind == "missing") db.Remove(customer);
        else Set(customer, kind == "other-tenant" ? "TenantId" : "Status",
            kind == "other-tenant" ? Guid.NewGuid() : "INACTIVE");
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var result = await Service(db).NotifyAsync(Context(f), f.OutletId, f.Order.Id, default);
        Assert.Contains(result.Error.Code, new[] { "online_orders.notification_recipient_unavailable", "online_orders.not_found" });
        Assert.Empty(await db.NotificationEvents.ToListAsync());
        Assert.Equal("READY", (await db.FulfillmentOrders.SingleAsync()).FulfillmentStatus);
    }

    [Fact]
    public async Task Notify_PersistsInAppAudit_SequentialDuplicateAndTimeoutRetryReuseResult()
    {
        await using var db = CreateDb();
        var f = Seed(db);
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal("READY_FOR_COLLECTION", (await db.SalesOrders.SingleAsync()).GetClickAndCollectCustomerStatus());
        Assert.Empty(await db.NotificationEvents.ToListAsync());
        var service = Service(db);
        var first = await service.NotifyAsync(Context(f), f.OutletId, f.Order.Id, default);
        Assert.True(first.IsSuccess, first.Error?.Code);
        db.ChangeTracker.Clear();
        var second = await service.NotifyAsync(Context(f), f.OutletId, f.Order.Id, default);
        db.ChangeTracker.Clear();
        var afterLostResponse = await service.NotifyAsync(Context(f), f.OutletId, f.Order.Id, default);
        Assert.True(second.IsSuccess); Assert.True(afterLostResponse.IsSuccess);
        Assert.True(second.Value!.AlreadyExisted); Assert.Equal(0, second.Value.CreatedMessageCount);
        Assert.Equal(first.Value!.EventId, afterLostResponse.Value!.EventId);
        var evt = Assert.Single(await db.NotificationEvents.ToListAsync());
        Assert.Equal("ecommerce.order_ready_for_collection", evt.EventCode);
        Assert.Equal(f.UserId, evt.CreatedByTenantUserId); Assert.Equal(Now, evt.CreatedAt);
        Assert.Equal(f.TenantId, evt.TenantId);
        var message = Assert.Single(await db.NotificationMessages.ToListAsync());
        Assert.Equal(f.Order.CustomerId, message.CustomerId);
        Assert.Equal("IN_APP", message.ChannelType); Assert.Equal("DELIVERED", message.MessageStatus);
        Assert.Equal(Now, message.DeliveredAt);
        Assert.Single(await db.NotificationInboxItems.ToListAsync());
        await AssertUnchanged(db, f);
    }

    [Fact]
    public async Task Notify_ServiceFailure_KeepsReadyAndTracking()
    {
        await using var db = CreateDb();
        var f = Seed(db);
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var result = await Service(db, new FailingNotification()).NotifyAsync(Context(f), f.OutletId, f.Order.Id, default);
        Assert.Equal("online_orders.notification_failed", result.Error.Code);
        Assert.Empty(await db.NotificationEvents.ToListAsync());
        await AssertUnchanged(db, f);
    }

    internal static async Task AssertUnchanged(EPosDbContext db, PackingFixture f)
    {
        db.ChangeTracker.Clear();
        var fulfillment = await db.FulfillmentOrders.SingleAsync(x => x.Id == f.Fulfillment.Id);
        Assert.Equal("READY", fulfillment.FulfillmentStatus); Assert.Equal(Now, fulfillment.ReadyAt);
        Assert.Equal(5, fulfillment.RowVersion);
        var pickup = await db.PickupOrders.SingleAsync(x => x.Id == f.Pickup.Id);
        Assert.Equal("READY", pickup.PickupStatus); Assert.Null(pickup.CollectedAt);
        var line = await db.FulfillmentOrderLines.SingleAsync(x => x.Id == f.FulfillmentLine.Id);
        Assert.Equal(2, line.PickedQuantity); Assert.Equal(2, line.PackedQuantity);
        Assert.Empty(await db.FulfillmentOrderEvents.ToListAsync());
        Assert.Empty(await db.PickupOrderEvents.ToListAsync());
        Assert.Equal("READY_FOR_COLLECTION", (await db.SalesOrders.SingleAsync(x => x.Id == f.Order.Id)).GetClickAndCollectCustomerStatus());
    }

    internal static PackingFixture Seed(EPosDbContext db, string state = "READY")
    {
        var f = SeedPackableAggregate(db, 3, 2, 5, 1, state);
        if (state == "READY") Set(f.Fulfillment, "ReadyAt", Now);
        Set(f.FulfillmentLine, "PackedQuantity", 2m);
        Set(f.Pickup, "PickupStatus", state == "READY" ? "READY" : "PENDING");
        Set(f.Order, "Status", "ACCEPTED");
        Set(f.Order, "FulfillmentStatus", state == "READY" ? "READY_FOR_COLLECTION" : "PREPARING");
        db.Customers.Add(E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer.CreatePosCustomer(f.Order.CustomerId!.Value, f.TenantId,
            "OO06-CUSTOMER", "Test customer", "", null, f.UserId, Now));
        return f;
    }

    internal static readonly string[] Permissions = [
        OnlineOrderPickingPermissions.OrdersAccess, OnlineOrderPickingPermissions.OrdersView,
        OnlineOrderPickingPermissions.CollectionViewReady, OnlineOrderPickingPermissions.CollectionNotifyCustomer];
    internal static TenantRequestContext Context(PackingFixture f) => new(f.TenantId, f.UserId, Permissions);
    internal static PosOnlineOrderReadyService Service(EPosDbContext db, INotificationService? notification = null)
    {
        var repo = new NotificationRepository(db);
        return new(new PosOnlineOrderPickingRepository(db),
            notification ?? new NotificationService(repo, [new InAppNotificationChannelHandler(repo)], new Clock()),
            new Entitlements(), new Clock());
    }
    private static Task<E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts.PosOnlineOrderPickingRepositoryResult> Get(
        EPosDbContext db, PackingFixture f) =>
        new PosOnlineOrderPickingRepository(db).GetAsync(f.TenantId, f.UserId, f.OutletId, f.Order.Id, Now, default);
    private static EPosDbContext CreateDb() => new(new DbContextOptionsBuilder<EPosDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static void Set(object target, string name, object? value) => target.GetType().GetProperty(name)!.SetValue(target, value);
    private sealed class Clock : IDateTimeProvider { public DateTimeOffset UtcNow => Now; }
    private sealed class FailingNotification : INotificationService
    {
        public Task<ApplicationResult<NotificationCreateResult>> CreateAsync(CreateNotificationEventRequest request, CancellationToken ct) =>
            throw new InvalidOperationException("Injected notification failure");
    }
    private sealed class Entitlements : ITenantFeatureEntitlementEvaluator
    {
        public Task<TenantFeatureEntitlementEvaluation> EvaluateAsync(Guid tenant, string feature, DateTimeOffset now, CancellationToken ct) =>
            Task.FromResult(TenantFeatureEntitlementEvaluation.Allowed(feature, feature, false, true, false));
        public Task<bool> IsEnabledAsync(Guid tenant, string feature, DateTimeOffset now, CancellationToken ct) => Task.FromResult(true);
    }
}
