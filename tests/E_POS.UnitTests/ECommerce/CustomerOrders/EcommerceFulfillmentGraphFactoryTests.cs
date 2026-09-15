using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class EcommerceFulfillmentGraphFactoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FulfillmentOrder_Create_InitializesPendingStatusAndVersionOne()
    {
        var tenantId = Guid.NewGuid();
        var salesOrderId = Guid.NewGuid();
        var fulfillmentMethodOutletId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        var fulfillmentOrder = FulfillmentOrder.Create(
            Guid.NewGuid(), tenantId, salesOrderId, "FUL-SO-1",
            fulfillmentMethodOutletId, locationId,
            DateOnly.FromDateTime(Now.Date), Now, Now);

        Assert.Equal(tenantId, fulfillmentOrder.TenantId);
        Assert.Equal(salesOrderId, fulfillmentOrder.SalesOrderId);
        Assert.Equal("FUL-SO-1", fulfillmentOrder.FulfillmentNumber);
        Assert.Equal(fulfillmentMethodOutletId, fulfillmentOrder.FulfillmentMethodOutletId);
        Assert.Equal(locationId, fulfillmentOrder.SourceInventoryLocationId);
        Assert.Equal("PENDING", fulfillmentOrder.FulfillmentStatus);
        Assert.Equal(1, fulfillmentOrder.RowVersion);
        Assert.Equal(Now, fulfillmentOrder.CreatedAt);
    }

    [Fact]
    public void FulfillmentOrder_Create_BlankFulfillmentNumber_Throws()
    {
        Assert.Throws<ArgumentException>(() => FulfillmentOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "  ",
            Guid.NewGuid(), null, null, null, Now));
    }

    [Fact]
    public void FulfillmentOrderLine_Create_InitializesPendingLineAtRequestedQuantity()
    {
        var fulfillmentOrderId = Guid.NewGuid();
        var salesOrderLineId = Guid.NewGuid();

        var line = FulfillmentOrderLine.Create(
            Guid.NewGuid(), Guid.NewGuid(), fulfillmentOrderId, salesOrderLineId, 3m, 0m, Now);

        Assert.Equal(fulfillmentOrderId, line.FulfillmentOrderId);
        Assert.Equal(salesOrderLineId, line.SalesOrderLineId);
        Assert.Equal(3m, line.RequestedQuantity);
        Assert.Equal(0m, line.PickedQuantity);
        Assert.Equal(0m, line.PackedQuantity);
        Assert.Equal(0m, line.FulfilledQuantity);
        Assert.Equal("PENDING", line.LineStatus);
    }

    [Fact]
    public void FulfillmentOrderLine_Create_NonPositiveQuantity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FulfillmentOrderLine.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, 0m, Now));
    }

    [Fact]
    public void PickupSlot_CreateOpen_InitializesOpenSlotAtZeroReservedCount()
    {
        var fulfillmentMethodOutletId = Guid.NewGuid();

        var slot = PickupSlot.CreateOpen(
            Guid.NewGuid(), Guid.NewGuid(), fulfillmentMethodOutletId, "ECOMM-SO-1",
            DateOnly.FromDateTime(Now.Date), new TimeOnly(11, 0), new TimeOnly(11, 30), 1, Now);

        Assert.Equal(fulfillmentMethodOutletId, slot.FulfillmentMethodOutletId);
        Assert.Equal("OPEN", slot.SlotStatus);
        Assert.Equal(0, slot.ReservedCount);
        Assert.Equal(1, slot.Capacity);
    }

    [Fact]
    public void PickupSlot_CreateOpen_WindowEndNotAfterStart_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PickupSlot.CreateOpen(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ECOMM-SO-1",
            DateOnly.FromDateTime(Now.Date), new TimeOnly(11, 30), new TimeOnly(11, 0), 1, Now));
    }

    [Fact]
    public void PickupSlot_Reserve_AtFullCapacity_FlipsStatusToFull()
    {
        var slot = PickupSlot.CreateOpen(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ECOMM-SO-1",
            DateOnly.FromDateTime(Now.Date), new TimeOnly(11, 0), new TimeOnly(11, 30), 1, Now);

        slot.Reserve(1, Now);

        Assert.Equal(1, slot.ReservedCount);
        Assert.Equal("FULL", slot.SlotStatus);
    }

    [Fact]
    public void PickupOrder_Create_InitializesPendingPickupWithSlotReservationLinked()
    {
        var fulfillmentOrderId = Guid.NewGuid();
        var slotReservationId = Guid.NewGuid();

        var pickupOrder = PickupOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(), fulfillmentOrderId, slotReservationId,
            "PU-SO-1", "Jane Doe", "+94770000000", "jane@example.com", "EMAIL", Now);

        Assert.Equal(fulfillmentOrderId, pickupOrder.FulfillmentOrderId);
        Assert.Equal(slotReservationId, pickupOrder.PickupSlotReservationId);
        Assert.Equal("PU-SO-1", pickupOrder.PickupNumber);
        Assert.Equal("Jane Doe", pickupOrder.PickupContactName);
        Assert.Equal("PENDING", pickupOrder.PickupStatus);
    }

    [Fact]
    public void PickupOrder_Create_BlankContactName_Throws()
    {
        Assert.Throws<ArgumentException>(() => PickupOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            "PU-SO-1", "  ", null, null, null, Now));
    }

    [Fact]
    public void PickupSlotReservation_CreatePendingThenConfirm_AttachesSalesOrder()
    {
        var pickupSlotId = Guid.NewGuid();
        var checkoutSessionId = Guid.NewGuid();
        var salesOrderId = Guid.NewGuid();

        var reservation = PickupSlotReservation.CreatePending(
            Guid.NewGuid(), Guid.NewGuid(), pickupSlotId, checkoutSessionId, 1, Now, Now);
        reservation.Confirm(salesOrderId, Now);

        Assert.Equal("CONFIRMED", reservation.ReservationStatus);
        Assert.Equal(salesOrderId, reservation.SalesOrderId);
        Assert.Equal(Now, reservation.ConfirmedAt);
    }
}
