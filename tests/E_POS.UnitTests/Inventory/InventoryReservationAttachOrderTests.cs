using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using Xunit;

namespace E_POS.UnitTests.Inventory;

public sealed class InventoryReservationAttachOrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AttachOrder_RepointsSourceReferenceAndClearsExpiry()
    {
        var checkoutSessionId = Guid.NewGuid();
        var salesOrderId = Guid.NewGuid();
        var reservation = InventoryReservation.Create(
            Guid.NewGuid(), Guid.NewGuid(), "RES-1", "CHECKOUT", checkoutSessionId,
            "CHK-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "PENDING",
            Now, Now.AddMinutes(15), null, Now);

        reservation.AttachOrder(salesOrderId, "SO-WEB-1", Now.AddMinutes(1));

        Assert.Equal(salesOrderId, reservation.SourceReferenceId);
        Assert.Equal("SO-WEB-1", reservation.SourceReferenceNumber);
        Assert.Null(reservation.ExpiresAt);
        Assert.Equal(Now.AddMinutes(1), reservation.UpdatedAt);
    }

    [Fact]
    public void AttachOrder_BlankOrderNumber_KeepsExistingReferenceNumber()
    {
        var reservation = InventoryReservation.Create(
            Guid.NewGuid(), Guid.NewGuid(), "RES-1", "CHECKOUT", Guid.NewGuid(),
            "CHK-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "PENDING",
            Now, Now.AddMinutes(15), null, Now);

        reservation.AttachOrder(Guid.NewGuid(), null, Now.AddMinutes(1));

        Assert.Equal("CHK-1", reservation.SourceReferenceNumber);
        Assert.Null(reservation.ExpiresAt);
    }
}
