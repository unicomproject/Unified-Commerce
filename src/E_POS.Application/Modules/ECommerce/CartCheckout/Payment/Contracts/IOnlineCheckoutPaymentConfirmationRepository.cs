namespace E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Contracts;

public interface IOnlineCheckoutPaymentConfirmationRepository
{
    Task<OnlineCheckoutPaymentConfirmationResult> ApplyCheckoutCompletedAsync(
        Guid tenantId,
        Guid salesOrderId,
        Guid salesPaymentId,
        decimal paidAmount,
        string currencyCode,
        string? providerSessionId,
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

/// <summary>
/// <paramref name="AnomalyCode"/> distinguishes a benign idempotent replay (null — the payment
/// already reached the exact outcome this call was asking for, safe to re-notify) from a genuine
/// problem (non-null — amount/currency/session mismatch, or the payment is in some other terminal
/// state than the one this call expected). Anomalies are never auto-applied and never notified;
/// they are surfaced for logging/manual review.
/// </summary>
public sealed record OnlineCheckoutPaymentConfirmationResult(
    bool Found,
    bool Applied,
    Guid CustomerId,
    Guid OrderId,
    string OrderNumber,
    string? AnomalyCode = null)
{
    public bool ShouldNotify => Found && AnomalyCode is null;

    public static OnlineCheckoutPaymentConfirmationResult NotFound() =>
        new(false, false, Guid.Empty, Guid.Empty, string.Empty);

    public static OnlineCheckoutPaymentConfirmationResult AlreadyProcessed(Guid customerId, Guid orderId, string orderNumber) =>
        new(true, false, customerId, orderId, orderNumber);

    public static OnlineCheckoutPaymentConfirmationResult Success(Guid customerId, Guid orderId, string orderNumber) =>
        new(true, true, customerId, orderId, orderNumber);

    public static OnlineCheckoutPaymentConfirmationResult Anomaly(
        Guid customerId, Guid orderId, string orderNumber, string anomalyCode) =>
        new(true, false, customerId, orderId, orderNumber, anomalyCode);
}
