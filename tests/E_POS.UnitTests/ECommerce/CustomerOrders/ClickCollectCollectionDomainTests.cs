using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class ClickCollectCollectionDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Pickup_IssueCollectionQr_WhenReady_StoresHashVersionAndExpiry()
    {
        var pickup = CreatePickup("READY");
        var expires = Now.AddDays(7);

        pickup.IssueCollectionQr("abc123hash", 1, expires, Now);

        Assert.Equal("abc123hash", pickup.PickupQrTokenHash);
        Assert.Equal(1, pickup.PickupQrVersion);
        Assert.Equal(expires, pickup.PickupQrExpiresAt);
    }

    [Fact]
    public void Pickup_IssueCollectionQr_WhenPending_Throws()
    {
        var pickup = CreatePickup("PENDING");
        Assert.Throws<InvalidOperationException>(() =>
            pickup.IssueCollectionQr("hash", 1, Now.AddDays(1), Now));
    }

    [Fact]
    public void Pickup_MarkCollected_ReadyToCollected()
    {
        var pickup = CreatePickup("READY");
        pickup.MarkCollected(Now);

        Assert.Equal("COLLECTED", pickup.PickupStatus);
        Assert.Equal(Now, pickup.CollectedAt);
    }

    [Fact]
    public void Pickup_MarkCollected_AlreadyCollected_IsIdempotent()
    {
        var pickup = CreatePickup("COLLECTED");
        Set(pickup, nameof(pickup.CollectedAt), Now.AddMinutes(-5));

        pickup.MarkCollected(Now);

        Assert.Equal("COLLECTED", pickup.PickupStatus);
        Assert.Equal(Now.AddMinutes(-5), pickup.CollectedAt);
    }

    [Fact]
    public void Pickup_MarkCollected_Cancelled_Throws()
    {
        var pickup = CreatePickup("CANCELLED");
        Assert.Throws<InvalidOperationException>(() => pickup.MarkCollected(Now));
    }

    [Fact]
    public void Fulfillment_MarkFulfilled_ReadyToFulfilled_BumpsVersion()
    {
        var fulfillment = CreateFulfillment("READY", version: 4);
        var userId = Guid.NewGuid();

        fulfillment.MarkFulfilled(userId, 4, Now);

        Assert.Equal("FULFILLED", fulfillment.FulfillmentStatus);
        Assert.Equal(Now, fulfillment.FulfilledAt);
        Assert.Equal(5, fulfillment.RowVersion);
        Assert.Equal(userId, fulfillment.UpdatedByTenantUserId);
    }

    [Fact]
    public void Fulfillment_MarkFulfilled_VersionConflict_Throws()
    {
        var fulfillment = CreateFulfillment("READY", version: 3);
        Assert.Throws<InvalidOperationException>(() =>
            fulfillment.MarkFulfilled(Guid.NewGuid(), 2, Now));
    }

    [Fact]
    public void SalesOrder_ApplyPosCollected_SetsCompletedAndCollected()
    {
        var order = CreateClickAndCollect();
        var userId = Guid.NewGuid();

        order.ApplyPosCollected(userId, Now);

        Assert.Equal("COMPLETED", order.Status);
        Assert.Equal("COLLECTED", order.FulfillmentStatus);
        Assert.Equal(Now, order.CompletedAt);
        Assert.Equal(userId, order.UpdatedByTenantUserId);
    }

    [Fact]
    public void SalesOrder_ApplyPosCollectionPayment_PartialThenPaid()
    {
        var order = CreateClickAndCollect(total: 1000m);
        var userId = Guid.NewGuid();

        order.ApplyPosCollectionPayment(400m, userId, Now);
        Assert.Equal(400m, order.PaidAmount);
        Assert.Equal(600m, order.BalanceDue);
        Assert.Equal("PARTIALLY_PAID", order.PaymentStatus);
        Assert.Null(order.CompletedAt);

        order.ApplyPosCollectionPayment(600m, userId, Now.AddMinutes(1));
        Assert.Equal(1000m, order.PaidAmount);
        Assert.Equal(0m, order.BalanceDue);
        Assert.Equal("PAID", order.PaymentStatus);
        Assert.Null(order.CompletedAt);
        Assert.NotEqual("COLLECTED", order.FulfillmentStatus);
    }

    [Fact]
    public void SalesOrder_ApplyPosCollectionPayment_RejectsOverpay()
    {
        var order = CreateClickAndCollect(total: 100m);
        Assert.Throws<InvalidOperationException>(() =>
            order.ApplyPosCollectionPayment(150m, Guid.NewGuid(), Now));
    }

    [Fact]
    public void SalesOrder_ApplyPosCollectionPayment_RejectsAlreadyPaid()
    {
        var order = CreateClickAndCollect(total: 100m);
        order.ApplyPosCollectionPayment(100m, Guid.NewGuid(), Now);

        Assert.Throws<InvalidOperationException>(() =>
            order.ApplyPosCollectionPayment(1m, Guid.NewGuid(), Now.AddMinutes(1)));
    }

    [Fact]
    public void Validate_DoesNotEqual_Collected_And_Payment_DoesNotCollect()
    {
        var pickup = CreatePickup("READY");
        pickup.IssueCollectionQr("hash", 1, Now.AddDays(7), Now);
        Assert.Null(pickup.CollectedAt);
        Assert.Equal("READY", pickup.PickupStatus);

        var order = CreateClickAndCollect(total: 250m);
        order.ApplyPosCollectionPayment(250m, Guid.NewGuid(), Now);
        Assert.Equal("PAID", order.PaymentStatus);
        Assert.Null(order.CompletedAt);
        Assert.NotEqual("COLLECTED", order.FulfillmentStatus);
    }

    private static PickupOrder CreatePickup(string status)
    {
        var pickup = Create<PickupOrder>();
        Set(pickup, nameof(pickup.Id), Guid.NewGuid());
        Set(pickup, nameof(pickup.TenantId), Guid.NewGuid());
        Set(pickup, nameof(pickup.FulfillmentOrderId), Guid.NewGuid());
        Set(pickup, nameof(pickup.PickupNumber), "PU-1");
        Set(pickup, nameof(pickup.PickupContactName), "Customer");
        Set(pickup, nameof(pickup.PickupStatus), status);
        Set(pickup, nameof(pickup.CreatedAt), Now);
        return pickup;
    }

    private static FulfillmentOrder CreateFulfillment(string status, long version)
    {
        var fulfillment = Create<FulfillmentOrder>();
        Set(fulfillment, nameof(fulfillment.Id), Guid.NewGuid());
        Set(fulfillment, nameof(fulfillment.TenantId), Guid.NewGuid());
        Set(fulfillment, nameof(fulfillment.SalesOrderId), Guid.NewGuid());
        Set(fulfillment, nameof(fulfillment.FulfillmentNumber), "FUL-1");
        Set(fulfillment, nameof(fulfillment.FulfillmentMethodOutletId), Guid.NewGuid());
        Set(fulfillment, nameof(fulfillment.FulfillmentStatus), status);
        Set(fulfillment, nameof(fulfillment.RowVersion), version);
        Set(fulfillment, nameof(fulfillment.ReadyAt), Now);
        Set(fulfillment, nameof(fulfillment.CreatedAt), Now);
        return fulfillment;
    }

    private static SalesOrder CreateClickAndCollect(decimal total = 2000m) =>
        SalesOrder.CreateClickAndCollect(
            Guid.NewGuid(), Guid.NewGuid(), "EC-COLLECT-1", "idem-collect-1", Guid.NewGuid(), Guid.NewGuid(),
            "CLICK_COLLECT", Guid.NewGuid(), "MAIN", "Main Store", Guid.NewGuid(), "Test Customer",
            "customer@example.com", "+94110000000", "LKR", false, total, 0m, 0m, 0m, total,
            Now.AddHours(2), Now.AddHours(3), "Asia/Colombo", Now);

    private static T Create<T>() where T : class =>
        (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

    private static void Set<T>(T target, string property, object? value) where T : class =>
        typeof(T).GetProperty(property)!.SetValue(target, value);
}
