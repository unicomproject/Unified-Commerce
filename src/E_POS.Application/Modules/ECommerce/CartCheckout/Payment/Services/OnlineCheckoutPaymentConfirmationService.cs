using E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Notifications;
using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;

namespace E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Services;

public sealed class OnlineCheckoutPaymentConfirmationService : IOnlineCheckoutPaymentConfirmationService
{
    private readonly IOnlineCheckoutPaymentConfirmationRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly ITenantStaffNotificationRecipientRepository _staffNotificationRecipientRepository;

    public OnlineCheckoutPaymentConfirmationService(
        IOnlineCheckoutPaymentConfirmationRepository repository,
        INotificationService notificationService,
        ITenantStaffNotificationRecipientRepository staffNotificationRecipientRepository)
    {
        _repository = repository;
        _notificationService = notificationService;
        _staffNotificationRecipientRepository = staffNotificationRecipientRepository;
    }

    public async Task HandleCheckoutCompletedAsync(
        Guid tenantId,
        Guid salesOrderId,
        Guid salesPaymentId,
        decimal paidAmount,
        string? externalReference,
        string? providerResponseJson,
        CancellationToken cancellationToken)
    {
        var result = await _repository.ApplyCheckoutCompletedAsync(
            tenantId,
            salesOrderId,
            salesPaymentId,
            paidAmount,
            externalReference,
            providerResponseJson,
            DateTimeOffset.UtcNow,
            cancellationToken);

        if (!result.Found || !result.Applied) return;

        await _notificationService.CreateAsync(
            ECommerceOrderNotificationFactory.OrderPaymentSucceeded(
                tenantId, result.CustomerId, result.OrderId, result.OrderNumber),
            cancellationToken);

        var staffTenantUserIds = await _staffNotificationRecipientRepository.GetActiveStaffTenantUserIdsAsync(
            tenantId, cancellationToken);
        foreach (var staffTenantUserId in staffTenantUserIds)
        {
            await _notificationService.CreateAsync(
                ECommerceOrderNotificationFactory.OrderPaymentSucceededForStaff(
                    tenantId, staffTenantUserId, result.OrderId, result.OrderNumber),
                cancellationToken);
        }
    }

    public async Task HandleCheckoutExpiredAsync(
        Guid tenantId,
        Guid salesOrderId,
        Guid salesPaymentId,
        string reason,
        CancellationToken cancellationToken)
    {
        var result = await _repository.ApplyCheckoutExpiredAsync(
            tenantId,
            salesOrderId,
            salesPaymentId,
            reason,
            DateTimeOffset.UtcNow,
            cancellationToken);

        if (!result.Found || !result.Applied) return;

        await _notificationService.CreateAsync(
            ECommerceOrderNotificationFactory.OrderStatusChanged(
                tenantId, result.CustomerId, result.OrderId, result.OrderNumber, "CANCELLED"),
            cancellationToken);
    }
}
