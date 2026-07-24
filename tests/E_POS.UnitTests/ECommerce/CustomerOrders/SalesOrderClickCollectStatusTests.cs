using E_POS.Domain.Modules.Tenant.Orders.Entities;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class SalesOrderClickCollectStatusTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UpdateClickAndCollectStatus_PendingToAccepted_EnablesAcceptedStatus()
    {
        var order = CreateOrder();

        order.UpdateClickAndCollectStatus("ACCEPTED", Guid.NewGuid(), Now.AddMinutes(5));

        Assert.Equal("ACCEPTED", order.Status);
        Assert.Equal("ACCEPTED", order.FulfillmentStatus);
        Assert.Equal("ACCEPTED", order.GetClickAndCollectCustomerStatus());
    }

    [Fact]
    public void UpdateClickAndCollectStatus_PendingToReady_ThrowsInvalidTransition()
    {
        var order = CreateOrder();

        Assert.Throws<InvalidOperationException>(() =>
            order.UpdateClickAndCollectStatus("READY_FOR_COLLECTION", Guid.NewGuid(), Now.AddMinutes(5)));
    }

    [Fact]
    public void CancelClickAndCollectByCustomer_PendingOrder_MarksCancelled()
    {
        var order = CreateOrder();

        order.CancelClickAndCollectByCustomer("Changed my mind", Now.AddMinutes(5));

        Assert.Equal("CANCELLED", order.Status);
        Assert.Equal("CANCELLED", order.FulfillmentStatus);
        Assert.Equal("CANCELLED", order.GetClickAndCollectCustomerStatus());
        Assert.Equal(Now.AddMinutes(5), order.CancelledAt);
        Assert.Equal("Changed my mind", order.CancellationReason);
    }

    [Fact]
    public void CancelClickAndCollectByCustomer_AcceptedOrder_MarksCancelled()
    {
        var order = CreateOrder();
        order.UpdateClickAndCollectStatus("ACCEPTED", Guid.NewGuid(), Now.AddMinutes(1));

        order.CancelClickAndCollectByCustomer(null, Now.AddMinutes(5));

        Assert.Equal("CANCELLED", order.Status);
        Assert.Equal("CANCELLED", order.FulfillmentStatus);
        Assert.Equal(Now.AddMinutes(5), order.CancelledAt);
        Assert.Null(order.CancellationReason);
    }

    [Fact]
    public void CancelClickAndCollectByCustomer_PreparingOrder_ThrowsInvalidTransition()
    {
        var order = CreateOrder();
        order.UpdateClickAndCollectStatus("ACCEPTED", Guid.NewGuid(), Now.AddMinutes(1));
        order.UpdateClickAndCollectStatus("PREPARING", Guid.NewGuid(), Now.AddMinutes(2));

        Assert.Throws<InvalidOperationException>(() =>
            order.CancelClickAndCollectByCustomer("Too late", Now.AddMinutes(5)));
    }

    private static SalesOrder CreateOrder() => SalesOrder.CreateClickAndCollect(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "SO-WEB-1",
        "confirm-1",
        Guid.NewGuid(),
        Guid.NewGuid(),
        "CLICK_COLLECT",
        Guid.NewGuid(),
        "OUT-1",
        "Etihad Stadium Store",
        Guid.NewGuid(),
        "Customer",
        "customer@example.com",
        null,
        "GBP",
        false,
        49.99m,
        0m,
        0m,
        0m,
        49.99m,
        Now.AddDays(1),
        Now.AddDays(1).AddMinutes(30),
        "Europe/London",
        Now);
}

