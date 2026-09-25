namespace E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Contracts;

public interface IOnlineCheckoutPaymentConfirmationService
{
    Task HandleCheckoutCompletedAsync(
        Guid tenantId,
        Guid salesOrderId,
        Guid salesPaymentId,
        decimal paidAmount,
        string currencyCode,
        string? providerSessionId,
        string? externalReference,
        string? providerResponseJson,
        CancellationToken cancellationToken);

    Task HandleCheckoutExpiredAsync(
        Guid tenantId,
        Guid salesOrderId,
        Guid salesPaymentId,
        string reason,
        CancellationToken cancellationToken);
}
