namespace E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Contracts;

public interface IOnlineCheckoutPaymentConfirmationRepository
{
    Task<OnlineCheckoutPaymentConfirmationResult> ApplyCheckoutCompletedAsync(
        Guid tenantId,
        Guid salesOrderId,
        Guid salesPaymentId,
        decimal paidAmount,
        string? externalReference,
        string? providerResponseJson,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<OnlineCheckoutPaymentConfirmationResult> ApplyCheckoutExpiredAsync(
        Guid tenantId,
        Guid salesOrderId,
        Guid salesPaymentId,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record OnlineCheckoutPaymentConfirmationResult(
    bool Found,
    bool Applied,
    Guid CustomerId,
    Guid OrderId,
    string OrderNumber)
{
    public static OnlineCheckoutPaymentConfirmationResult NotFound() => new(false, false, Guid.Empty, Guid.Empty, string.Empty);

    public static OnlineCheckoutPaymentConfirmationResult AlreadyProcessed(Guid customerId, Guid orderId, string orderNumber) =>
        new(true, false, customerId, orderId, orderNumber);

    public static OnlineCheckoutPaymentConfirmationResult Success(Guid customerId, Guid orderId, string orderNumber) =>
        new(true, true, customerId, orderId, orderNumber);
}
