using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using Xunit;

namespace E_POS.UnitTests.ECommerce;

public sealed class OnlineOrderFulfillmentDomainTests
{
    [Fact]
    public void Fulfillment_order_enforces_prepare_state_order_and_idempotent_replay()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var order = FulfillmentOrder.StartForClickAndCollect(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "FUL-0001",
            Guid.NewGuid(), Guid.NewGuid(), userId, now);

        Assert.Throws<InvalidOperationException>(() => order.MarkPacked(userId, now));

        order.MarkPicked(userId, now.AddMinutes(1));
        var pickedVersion = order.RowVersion;
        order.MarkPicked(userId, now.AddMinutes(2));

        Assert.Equal("PICKED", order.FulfillmentStatus);
        Assert.Equal(pickedVersion, order.RowVersion);

        order.MarkPacked(userId, now.AddMinutes(3));
        order.MarkReady(userId, now.AddMinutes(4));

        Assert.Equal("READY", order.FulfillmentStatus);
        Assert.NotNull(order.PackedAt);
        Assert.NotNull(order.ReadyAt);
    }

    [Fact]
    public void Fulfillment_line_rejects_over_pick_and_pack_before_full_pick()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var line = FulfillmentOrderLine.CreateForPicking(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2m,
            now, Guid.NewGuid());

        line.Pick(1m, userId, now.AddMinutes(1));

        Assert.Equal("PICKING", line.LineStatus);
        Assert.Throws<InvalidOperationException>(() => line.Pack(userId, now));
        Assert.Throws<InvalidOperationException>(() => line.Pick(2m, userId, now));

        line.Pick(1m, userId, now.AddMinutes(2));
        line.Pack(userId, now.AddMinutes(3));

        Assert.Equal("PACKED", line.LineStatus);
        Assert.Equal(2m, line.PackedQuantity);
        Assert.NotNull(line.InventoryReservationLineId);
    }
}
