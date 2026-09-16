using System.Reflection;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class FulfillmentPickingDomainTests
{
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Pick_ValidIncrement_UpdatesQuantityActorAndLineStatus()
    {
        var line = Line(requested: 3, picked: 1);

        line.Pick(2, ActorId, Now);

        Assert.Equal(3, line.PickedQuantity);
        Assert.Equal("PICKED", line.LineStatus);
        Assert.Equal(ActorId, line.PickedByTenantUserId);
    }

    [Fact]
    public void Pick_OverPick_IsRejectedWithoutMutation()
    {
        var line = Line(requested: 2, picked: 1);

        var error = Assert.Throws<InvalidOperationException>(() => line.Pick(2, ActorId, Now));

        Assert.Equal("FULFILLMENT_PICK_QUANTITY_EXCEEDED", error.Message);
        Assert.Equal(1, line.PickedQuantity);
    }

    [Fact]
    public void RecordPickingMutation_PickingAndMatchingVersion_IncrementsExactlyOnce()
    {
        var fulfillment = Fulfillment("PICKING", 4);

        fulfillment.RecordPickingMutation(ActorId, 4, Now);

        Assert.Equal(5, fulfillment.RowVersion);
        Assert.Equal("PICKING", fulfillment.FulfillmentStatus);
    }

    [Theory]
    [InlineData("PENDING")]
    [InlineData("ALLOCATED")]
    [InlineData("PICKED")]
    [InlineData("PACKED")]
    [InlineData("READY")]
    [InlineData("FULFILLED")]
    [InlineData("CANCELLED")]
    public void RecordPickingMutation_InvalidLifecycle_IsRejected(string status)
    {
        var fulfillment = Fulfillment(status, 4);

        var error = Assert.Throws<InvalidOperationException>(() =>
            fulfillment.RecordPickingMutation(ActorId, 4, Now));

        Assert.Equal("FULFILLMENT_NOT_PICKABLE", error.Message);
        Assert.Equal(4, fulfillment.RowVersion);
    }

    [Fact]
    public void RecordPickingMutation_StaleVersion_IsRejectedWithoutMutation()
    {
        var fulfillment = Fulfillment("PICKING", 5);

        var error = Assert.Throws<InvalidOperationException>(() =>
            fulfillment.RecordPickingMutation(ActorId, 4, Now));

        Assert.Equal("FULFILLMENT_VERSION_CONFLICT", error.Message);
        Assert.Equal(5, fulfillment.RowVersion);
    }

    [Fact]
    public void AddPickingNote_PickingAndMatchingVersion_OnlyUpdatesAggregateVersion()
    {
        var fulfillment = Fulfillment("PICKING", 4);

        fulfillment.AddPickingNote(ActorId, 4, Now);

        Assert.Equal(5, fulfillment.RowVersion);
        Assert.Equal("PICKING", fulfillment.FulfillmentStatus);
    }

    [Theory]
    [InlineData("PACKED")]
    [InlineData("READY")]
    [InlineData("FULFILLED")]
    [InlineData("CANCELLED")]
    public void AddPickingNote_TerminalOrLaterLifecycle_IsRejected(string status)
    {
        var fulfillment = Fulfillment(status, 4);

        Assert.Throws<InvalidOperationException>(() =>
            fulfillment.AddPickingNote(ActorId, 4, Now));
        Assert.Equal(4, fulfillment.RowVersion);
    }

    [Fact]
    public void Pack_PickingMatchingVersion_TransitionsToPackedOnce()
    {
        var fulfillment = Fulfillment("PICKING", 4);

        fulfillment.Pack(ActorId, 4, Now);

        Assert.Equal("PACKED", fulfillment.FulfillmentStatus);
        Assert.Equal(5, fulfillment.RowVersion);
        Assert.Equal(Now, fulfillment.PackedAt);
        Assert.Null(fulfillment.ReadyAt);
    }

    [Fact]
    public void Pack_DoesNotMarkReady()
    {
        var fulfillment = Fulfillment("PICKING", 4);

        fulfillment.Pack(ActorId, 4, Now);

        Assert.Equal("PACKED", fulfillment.FulfillmentStatus);
        Assert.Null(fulfillment.ReadyAt);
    }

    [Theory]
    [InlineData("PACKED")]
    [InlineData("READY")]
    [InlineData("FULFILLED")]
    [InlineData("CANCELLED")]
    public void Pack_InvalidLifecycle_IsRejected(string status)
    {
        var fulfillment = Fulfillment(status, 4);

        var error = Assert.Throws<InvalidOperationException>(() => fulfillment.Pack(ActorId, 4, Now));

        Assert.Equal("FULFILLMENT_NOT_PACKABLE", error.Message);
        Assert.Equal(4, fulfillment.RowVersion);
    }

    [Fact]
    public void MarkReady_PackedMatchingVersion_TransitionsToReadyOnce()
    {
        var fulfillment = Fulfillment("PACKED", 6);
        Set(fulfillment, nameof(FulfillmentOrder.PackedAt), Now.AddMinutes(-5));

        fulfillment.MarkReady(ActorId, 6, Now);

        Assert.Equal("READY", fulfillment.FulfillmentStatus);
        Assert.Equal(7, fulfillment.RowVersion);
        Assert.Equal(Now, fulfillment.ReadyAt);
        Assert.Equal(Now.AddMinutes(-5), fulfillment.PackedAt);
    }

    [Fact]
    public void MarkReady_FromPicking_IsRejected()
    {
        var fulfillment = Fulfillment("PICKING", 4);

        var error = Assert.Throws<InvalidOperationException>(() =>
            fulfillment.MarkReady(ActorId, 4, Now));

        Assert.Equal("FULFILLMENT_NOT_READYABLE", error.Message);
        Assert.Equal(4, fulfillment.RowVersion);
        Assert.Null(fulfillment.ReadyAt);
    }

    [Fact]
    public void LinePack_SetsPackedQuantityEqualToPickedWithoutExceeding()
    {
        var line = Line(requested: 3, picked: 3);

        line.Pack(ActorId, Now);

        Assert.Equal(3, line.PackedQuantity);
        Assert.Equal("PACKED", line.LineStatus);
        Assert.Equal(ActorId, line.PackedByTenantUserId);
    }

    [Fact]
    public void LinePack_WhenNotFullyPicked_IsRejected()
    {
        var line = Line(requested: 3, picked: 1);

        var error = Assert.Throws<InvalidOperationException>(() => line.Pack(ActorId, Now));

        Assert.Equal("FULFILLMENT_PACK_NOT_READY", error.Message);
        Assert.Equal(0, line.PackedQuantity);
    }

    [Fact]
    public void LinePack_FullyCancelledLine_AllowsZeroPackedQuantity()
    {
        var line = Line(requested: 2, picked: 0);
        Set(line, nameof(FulfillmentOrderLine.CancelledQuantity), 2m);

        line.Pack(ActorId, Now);

        Assert.Equal(0, line.PackedQuantity);
        Assert.Equal("PACKED", line.LineStatus);
    }

    private static FulfillmentOrderLine Line(decimal requested, decimal picked)
    {
        var line = Create<FulfillmentOrderLine>();
        Set(line, nameof(FulfillmentOrderLine.RequestedQuantity), requested);
        Set(line, nameof(FulfillmentOrderLine.PickedQuantity), picked);
        Set(line, nameof(FulfillmentOrderLine.CancelledQuantity), 0m);
        Set(line, nameof(FulfillmentOrderLine.LineStatus), "PICKING");
        return line;
    }

    private static FulfillmentOrder Fulfillment(string status, long version)
    {
        var fulfillment = Create<FulfillmentOrder>();
        Set(fulfillment, nameof(FulfillmentOrder.FulfillmentStatus), status);
        Set(fulfillment, nameof(FulfillmentOrder.RowVersion), version);
        return fulfillment;
    }

    private static T Create<T>() where T : class =>
        (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

    private static void Set<T>(object instance, string property, T value) =>
        instance.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(instance, value);
}
