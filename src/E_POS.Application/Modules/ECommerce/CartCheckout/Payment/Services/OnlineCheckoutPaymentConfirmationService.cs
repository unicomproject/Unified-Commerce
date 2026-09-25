using E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Notifications;
using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;
using E_POS.Application.Modules.Shared.Notification.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Services;

public sealed class OnlineCheckoutPaymentConfirmationService : IOnlineCheckoutPaymentConfirmationService
{
    private readonly IOnlineCheckoutPaymentConfirmationRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly ITenantStaffNotificationRecipientRepository _staffNotificationRecipientRepository;
    private readonly ILogger<OnlineCheckoutPaymentConfirmationService> _logger;

    public OnlineCheckoutPaymentConfirmationService(
        IOnlineCheckoutPaymentConfirmationRepository repository,
        INotificationService notificationService,
        ITenantStaffNotificationRecipientRepository staffNotificationRecipientRepository,
        ILogger<OnlineCheckoutPaymentConfirmationService> logger)
    {
        _repository = repository;
        _notificationService = notificationService;
        _staffNotificationRecipientRepository = staffNotificationRecipientRepository;
        _logger = logger;
    }

    public async Task HandleCheckoutCompletedAsync(
        Guid tenantId,
        Guid salesOrderId,
        Guid salesPaymentId,
        decimal paidAmount,
        string currencyCode,
        string? providerSessionId,
        string? externalReference,
        string? providerResponseJson,
        CancellationToken cancellationToken)
    {
        var result = await _repository.ApplyCheckoutCompletedAsync(
            tenantId,
            salesOrderId,
            salesPaymentId,
            paidAmount,
            currencyCode,
            providerSessionId,
            externalReference,
            providerResponseJson,
            DateTimeOffset.UtcNow,
            cancellationToken);

        if (result.AnomalyCode is not null)
        {
            _logger.LogError(
                "Online checkout payment confirmation anomaly {AnomalyCode} for tenant {TenantId}, order {OrderId}, payment {PaymentId} — not applied, no notification sent.",
                result.AnomalyCode, tenantId, salesOrderId, salesPaymentId);
        }

        if (!result.ShouldNotify) return;

        // Notification creation is idempotent (deduplicated by deterministic event number), so
        // re-sending on every duplicate-but-benign webhook delivery — including a retry after a
        // prior attempt saved the payment but crashed before this point — safely heals rather
        // than duplicates.
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

        if (result.AnomalyCode is not null)
        {
            _logger.LogError(
                "Online checkout expiry confirmation anomaly {AnomalyCode} for tenant {TenantId}, order {OrderId}, payment {PaymentId} — not applied, no notification sent.",
                result.AnomalyCode, tenantId, salesOrderId, salesPaymentId);
        }

        if (!result.ShouldNotify) return;

        await _notificationService.CreateAsync(
            ECommerceOrderNotificationFactory.OrderStatusChanged(
                tenantId, result.CustomerId, result.OrderId, result.OrderNumber, "CANCELLED"),
            cancellationToken);
    }
}
