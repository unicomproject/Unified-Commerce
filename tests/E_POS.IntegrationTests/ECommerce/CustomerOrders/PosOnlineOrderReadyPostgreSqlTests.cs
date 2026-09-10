using E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.ECommerce.CustomerOrders;

/// <summary>Opt-in: sends only the approved seeded Development order's IN_APP notification.</summary>
public sealed class PosOnlineOrderReadyPostgreSqlTests
{
    [DevelopmentReadyFact]
    public async Task ConcurrentNotify_OneLogicalEventAndInbox_ReadyUnchanged()
    {
        var connectionString = Environment.GetEnvironmentVariable("OO06_DEVELOPMENT_CONNECTION")!;
        var parsed = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.Contains(parsed.Host, new[] { "localhost", "127.0.0.1" });
        Assert.Equal("UnifiedCommerceDb", parsed.Database);
        EPosDbContext Open() => new(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connectionString).Options);
        await using var probe = Open();
        var order = await probe.SalesOrders.AsNoTracking().SingleAsync(
            x => x.OrderNumber == "ECOMM-SEED-ACCEPTED-002");
        var fulfillment = await probe.FulfillmentOrders.AsNoTracking().SingleAsync(x => x.SalesOrderId == order.Id && x.TenantId == order.TenantId);
        var pickup = await probe.PickupOrders.AsNoTracking().SingleAsync(x => x.FulfillmentOrderId == fulfillment.Id && x.TenantId == order.TenantId);
        Assert.Equal("READY", fulfillment.FulfillmentStatus); Assert.NotNull(fulfillment.ReadyAt);
        Assert.Equal("READY", pickup.PickupStatus); Assert.Null(pickup.CollectedAt);
        var actor = await probe.TenantUsers.AsNoTracking().SingleAsync(
            x => x.TenantId == order.TenantId && x.Email == "CASHIER001@GMAIL.COM");
        var context = new E_POS.Application.Common.Models.TenantRequestContext(
            order.TenantId, actor.Id, PosOnlineOrderReadyRepositoryTests.Permissions);
        var beforeLines = await probe.FulfillmentOrderLines.AsNoTracking().Where(x => x.FulfillmentOrderId == fulfillment.Id)
            .OrderBy(x => x.Id).Select(x => new { x.Id, x.PickedQuantity, x.PackedQuantity }).ToListAsync();
        var beforeEvents = await probe.FulfillmentOrderEvents.CountAsync(x => x.FulfillmentOrderId == fulfillment.Id);
        var beforePickupEvents = await probe.PickupOrderEvents.CountAsync(x => x.PickupOrderId == pickup.Id);
        await using var firstDb = Open();
        await using var secondDb = Open();
        var results = await Task.WhenAll(
            PosOnlineOrderReadyRepositoryTests.Service(firstDb).NotifyAsync(context, order.ReportingOutletId!.Value, order.Id, default),
            PosOnlineOrderReadyRepositoryTests.Service(secondDb).NotifyAsync(context, order.ReportingOutletId!.Value, order.Id, default));
        Assert.All(results, r => Assert.True(r.IsSuccess, r.Error?.Code));
        Assert.Equal(results[0].Value!.EventId, results[1].Value!.EventId);
        Assert.Contains(results, r => r.Value!.AlreadyExisted);
        var key = $"ECOM-ORDER-READY-{order.Id:N}".ToUpperInvariant();
        var evt = await probe.NotificationEvents.AsNoTracking().SingleAsync(x => x.TenantId == order.TenantId && x.EventNumber == key);
        var message = await probe.NotificationMessages.AsNoTracking().SingleAsync(x => x.NotificationEventId == evt.Id && x.ChannelType == "IN_APP");
        Assert.Equal(order.CustomerId, message.CustomerId);
        Assert.Equal(1, await probe.NotificationInboxItems.CountAsync(x => x.NotificationMessageId == message.Id));
        var after = await probe.FulfillmentOrders.AsNoTracking().SingleAsync(x => x.Id == fulfillment.Id);
        var afterPickup = await probe.PickupOrders.AsNoTracking().SingleAsync(x => x.Id == pickup.Id);
        Assert.Equal("READY", after.FulfillmentStatus); Assert.Equal(fulfillment.ReadyAt, after.ReadyAt);
        Assert.Equal(fulfillment.RowVersion, after.RowVersion);
        Assert.Equal("READY", afterPickup.PickupStatus); Assert.Null(afterPickup.CollectedAt);
        Assert.Equal(beforeEvents, await probe.FulfillmentOrderEvents.CountAsync(x => x.FulfillmentOrderId == fulfillment.Id));
        Assert.Equal(beforePickupEvents, await probe.PickupOrderEvents.CountAsync(x => x.PickupOrderId == pickup.Id));
        var afterLines = await probe.FulfillmentOrderLines.AsNoTracking().Where(x => x.FulfillmentOrderId == fulfillment.Id)
            .OrderBy(x => x.Id).Select(x => new { x.Id, x.PickedQuantity, x.PackedQuantity }).ToListAsync();
        Assert.Equal(beforeLines, afterLines);
        var read = await new PosOnlineOrderPickingRepository(probe).GetAsync(
            order.TenantId, actor.Id, order.ReportingOutletId.Value, order.Id,
            DateTimeOffset.UtcNow, default);
        Assert.True(read.IsSuccess, read.ErrorCode);
        Assert.Equal("DELIVERED", read.Picking!.ReadyNotificationStatus);
        Assert.Equal("READY_FOR_COLLECTION", (await probe.SalesOrders.AsNoTracking().SingleAsync(x => x.Id == order.Id)).GetClickAndCollectCustomerStatus());
    }

    public sealed class DevelopmentReadyFactAttribute : FactAttribute
    {
        public DevelopmentReadyFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OO06_DEVELOPMENT_CONNECTION")))
                Skip = "Opt-in Development connection required; sends approved seeded order IN_APP notification.";
        }
    }
}
