using E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Services;
using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CartCheckout;

public sealed class OnlineCheckoutPaymentConfirmationServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid PaymentId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    [Fact]
    public async Task HandleCheckoutCompletedAsync_FreshSuccess_SendsCustomerAndStaffNotifications()
    {
        var repository = new Mock<IOnlineCheckoutPaymentConfirmationRepository>();
        repository.Setup(x => x.ApplyCheckoutCompletedAsync(
                TenantId, OrderId, PaymentId, 100m, "LKR", "cs_1", "pi_1", null,
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OnlineCheckoutPaymentConfirmationResult.Success(CustomerId, OrderId, "ORD-1"));

        var staffRepo = new Mock<ITenantStaffNotificationRecipientRepository>();
        var staffId = Guid.NewGuid();
        staffRepo.Setup(x => x.GetActiveStaffTenantUserIdsAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([staffId]);

        var notifications = new Mock<INotificationService>();
        var service = new OnlineCheckoutPaymentConfirmationService(
            repository.Object, notifications.Object, staffRepo.Object,
            NullLogger<OnlineCheckoutPaymentConfirmationService>.Instance);

        await service.HandleCheckoutCompletedAsync(
            TenantId, OrderId, PaymentId, 100m, "LKR", "cs_1", "pi_1", null, CancellationToken.None);

        notifications.Verify(x => x.CreateAsync(
            It.Is<Application.Modules.Shared.Notification.Dtos.CreateNotificationEventRequest>(
                r => r.EventCode == "ecommerce.order_payment_succeeded"),
            It.IsAny<CancellationToken>()), Times.Once);
        notifications.Verify(x => x.CreateAsync(
            It.Is<Application.Modules.Shared.Notification.Dtos.CreateNotificationEventRequest>(
                r => r.EventCode == "ecommerce.order_payment_succeeded.staff"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleCheckoutCompletedAsync_BenignDuplicate_StillSendsNotifications()
    {
        var repository = new Mock<IOnlineCheckoutPaymentConfirmationRepository>();
        repository.Setup(x => x.ApplyCheckoutCompletedAsync(
                TenantId, OrderId, PaymentId, 100m, "LKR", "cs_1", "pi_1", null,
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OnlineCheckoutPaymentConfirmationResult.AlreadyProcessed(CustomerId, OrderId, "ORD-1"));

        var staffRepo = new Mock<ITenantStaffNotificationRecipientRepository>();
        staffRepo.Setup(x => x.GetActiveStaffTenantUserIdsAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var notifications = new Mock<INotificationService>();
        var service = new OnlineCheckoutPaymentConfirmationService(
            repository.Object, notifications.Object, staffRepo.Object,
            NullLogger<OnlineCheckoutPaymentConfirmationService>.Instance);

        // A retry after a prior attempt saved the payment but crashed before notifying should
        // still be able to heal the missing notification — this is the whole point of gating on
        // ShouldNotify (Found + no anomaly) rather than Applied (fresh transition only).
        await service.HandleCheckoutCompletedAsync(
            TenantId, OrderId, PaymentId, 100m, "LKR", "cs_1", "pi_1", null, CancellationToken.None);

        notifications.Verify(x => x.CreateAsync(
            It.Is<Application.Modules.Shared.Notification.Dtos.CreateNotificationEventRequest>(
                r => r.EventCode == "ecommerce.order_payment_succeeded"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleCheckoutCompletedAsync_Anomaly_NeverNotifies()
    {
        var repository = new Mock<IOnlineCheckoutPaymentConfirmationRepository>();
        repository.Setup(x => x.ApplyCheckoutCompletedAsync(
                TenantId, OrderId, PaymentId, 999m, "LKR", "cs_1", "pi_1", null,
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OnlineCheckoutPaymentConfirmationResult.Anomaly(CustomerId, OrderId, "ORD-1", "amount_mismatch"));

        var staffRepo = new Mock<ITenantStaffNotificationRecipientRepository>(MockBehavior.Strict);
        var notifications = new Mock<INotificationService>(MockBehavior.Strict);
        var service = new OnlineCheckoutPaymentConfirmationService(
            repository.Object, notifications.Object, staffRepo.Object,
            NullLogger<OnlineCheckoutPaymentConfirmationService>.Instance);

        await service.HandleCheckoutCompletedAsync(
            TenantId, OrderId, PaymentId, 999m, "LKR", "cs_1", "pi_1", null, CancellationToken.None);

        notifications.VerifyNoOtherCalls();
        staffRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleCheckoutCompletedAsync_NotFound_NeverNotifies()
    {
        var repository = new Mock<IOnlineCheckoutPaymentConfirmationRepository>();
        repository.Setup(x => x.ApplyCheckoutCompletedAsync(
                TenantId, OrderId, PaymentId, 100m, "LKR", "cs_1", "pi_1", null,
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OnlineCheckoutPaymentConfirmationResult.NotFound());

        var staffRepo = new Mock<ITenantStaffNotificationRecipientRepository>(MockBehavior.Strict);
        var notifications = new Mock<INotificationService>(MockBehavior.Strict);
        var service = new OnlineCheckoutPaymentConfirmationService(
            repository.Object, notifications.Object, staffRepo.Object,
            NullLogger<OnlineCheckoutPaymentConfirmationService>.Instance);

        await service.HandleCheckoutCompletedAsync(
            TenantId, OrderId, PaymentId, 100m, "LKR", "cs_1", "pi_1", null, CancellationToken.None);

        notifications.VerifyNoOtherCalls();
        staffRepo.VerifyNoOtherCalls();
    }
}
