using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Services;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using E_POS.Application.Modules.Shared.Notification.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Constants;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using Moq;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class PosOnlineOrderReadyServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-09T08:00:00Z");
    private static readonly string[] Permissions =
    [
        OnlineOrderPickingPermissions.OrdersAccess, OnlineOrderPickingPermissions.OrdersView,
        OnlineOrderPickingPermissions.CollectionViewReady, OnlineOrderPickingPermissions.CollectionNotifyCustomer
    ];

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public async Task Notify_RequiresEveryPermission(int missing)
    {
        var repo = new Mock<IPosOnlineOrderReadyRepository>(MockBehavior.Strict);
        var service = Service(repo.Object, new Mock<INotificationService>().Object);
        var result = await service.NotifyAsync(
            new(Guid.NewGuid(), Guid.NewGuid(), Permissions.Where((_, i) => i != missing).ToArray()),
            Guid.NewGuid(), Guid.NewGuid(), default);
        Assert.Equal("online_orders.permission_denied", result.Error.Code);
        repo.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("PICKING", "PENDING", "ACCEPTED")]
    [InlineData("PACKED", "PENDING", "ACCEPTED")]
    [InlineData("READY", "COLLECTED", "ACCEPTED")]
    [InlineData("READY", "READY", "COMPLETED")]
    [InlineData("READY", "READY", "CANCELLED")]
    [InlineData("CANCELLED", "READY", "ACCEPTED")]
    [InlineData("FULFILLED", "COLLECTED", "COMPLETED")]
    public async Task Notify_RejectsNonReadyAggregate(string fulfillment, string pickup, string sales)
    {
        var repo = new CallbackRepository(fulfillment, pickup, sales);
        var notifications = new Mock<INotificationService>(MockBehavior.Strict);
        var result = await Service(repo, notifications.Object).NotifyAsync(Context(), Guid.NewGuid(), Guid.NewGuid(), default);
        Assert.Equal("online_orders.invalid_state", result.Error.Code);
        notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Notify_UsesServerRecipientActorAndCanonicalFactory()
    {
        var context = Context();
        var repo = new CallbackRepository();
        CreateNotificationEventRequest? captured = null;
        var notifications = new Mock<INotificationService>();
        notifications.Setup(x => x.CreateAsync(It.IsAny<CreateNotificationEventRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateNotificationEventRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(ApplicationResult<NotificationCreateResult>.Success(new() { CreatedMessageCount = 1 }));
        var result = await Service(repo, notifications.Object).NotifyAsync(context, Guid.NewGuid(), Guid.NewGuid(), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(repo.Order.CustomerId, captured!.Recipient.CustomerId);
        Assert.Equal(context.TenantId, captured.TenantId);
        Assert.Equal(context.UserId, captured.CreatedByTenantUserId);
        Assert.Equal("CUSTOMER", captured.Recipient.RecipientType);
        Assert.Equal("ecommerce.order_ready_for_collection", captured.EventCode);
        Assert.Equal($"ECOM-ORDER-READY-{repo.Order.Id:N}", captured.EventNumber);
        Assert.Equal("READY", repo.Fulfillment.FulfillmentStatus);
        Assert.Equal(Now, repo.Fulfillment.ReadyAt);
        Assert.Null(repo.Pickup.CollectedAt);
    }

    [Fact]
    public async Task Notify_ServiceFailure_IsSafeAndDoesNotMutateLifecycle()
    {
        var repo = new CallbackRepository();
        var notifications = new Mock<INotificationService>();
        notifications.Setup(x => x.CreateAsync(It.IsAny<CreateNotificationEventRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<NotificationCreateResult>.Failure(new("internal.failure", "private detail")));
        var result = await Service(repo, notifications.Object).NotifyAsync(Context(), Guid.NewGuid(), Guid.NewGuid(), default);
        Assert.Equal("online_orders.notification_failed", result.Error.Code);
        Assert.DoesNotContain("private detail", result.Error.Message);
        Assert.Equal("READY", repo.Fulfillment.FulfillmentStatus);
        Assert.Equal(Now, repo.Fulfillment.ReadyAt);
        Assert.Null(repo.Pickup.CollectedAt);
        Assert.Equal(1, repo.Fulfillment.RowVersion);
    }

    [Fact]
    public async Task Notify_RequiresEntitlement()
    {
        var repo = new Mock<IPosOnlineOrderReadyRepository>(MockBehavior.Strict);
        var result = await Service(repo.Object, new Mock<INotificationService>().Object, false)
            .NotifyAsync(Context(), Guid.NewGuid(), Guid.NewGuid(), default);
        Assert.Equal("online_orders.feature_not_entitled", result.Error.Code);
    }

    private static TenantRequestContext Context() => new(Guid.NewGuid(), Guid.NewGuid(), Permissions);
    private static PosOnlineOrderReadyService Service(IPosOnlineOrderReadyRepository repo, INotificationService notifications, bool allowed = true)
    {
        var entitlements = new Mock<ITenantFeatureEntitlementEvaluator>();
        entitlements.Setup(x => x.EvaluateAsync(It.IsAny<Guid>(), It.IsAny<string>(), Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allowed
                ? TenantFeatureEntitlementEvaluation.Allowed("click_collect", "click_collect", false, true, false)
                : TenantFeatureEntitlementEvaluation.Denied(TenantFeatureEntitlementDecision.Disabled,
                    "click_collect", "click_collect", false, true, false, "Disabled"));
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(x => x.UtcNow).Returns(Now);
        return new(repo, notifications, entitlements.Object, clock.Object);
    }

    private sealed class CallbackRepository : IPosOnlineOrderReadyRepository
    {
        public SalesOrder Order { get; } = new();
        public FulfillmentOrder Fulfillment { get; } = (FulfillmentOrder)Activator.CreateInstance(typeof(FulfillmentOrder), nonPublic: true)!;
        public PickupOrder Pickup { get; } = new();
        public CallbackRepository(string fulfillment = "READY", string pickup = "READY", string sales = "ACCEPTED")
        {
            Set(Order, "Id", Guid.NewGuid()); Set(Order, "CustomerId", Guid.NewGuid());
            Set(Order, "OrderNumber", "TEST-READY"); Set(Order, "Status", sales);
            Set(Fulfillment, "FulfillmentStatus", fulfillment); Set(Fulfillment, "ReadyAt", Now);
            Set(Pickup, "PickupStatus", pickup);
        }
        public Task<ApplicationResult<NotificationCreateResult>> ExecuteNotificationAsync(
            Guid tenantId, Guid tenantUserId, Guid outletId, Guid orderId,
            Func<SalesOrder, FulfillmentOrder, PickupOrder?, CancellationToken, Task<ApplicationResult<NotificationCreateResult>>> notify,
            CancellationToken cancellationToken) => notify(Order, Fulfillment, Pickup, cancellationToken);
    }
    private static void Set(object target, string name, object value) => target.GetType().GetProperty(name)!.SetValue(target, value);
}
