namespace E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Contracts;

public interface IOnlineCheckoutPaymentGateway
{
    Task<OnlineCheckoutSessionResult> CreateCheckoutSessionAsync(
        OnlineCheckoutSessionRequest request,
        CancellationToken cancellationToken);
}

public sealed record OnlineCheckoutSessionRequest(
    Guid TenantId,
    Guid CheckoutSessionId,
    Guid SalesOrderId,
    Guid SalesPaymentId,
    string OrderNumber,
    decimal Amount,
    string CurrencyCode,
    string? CustomerEmail);

public sealed record OnlineCheckoutSessionResult(
    bool Success,
    string? CheckoutUrl,
    string? ProviderSessionId,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static OnlineCheckoutSessionResult Succeeded(string checkoutUrl, string providerSessionId) =>
        new(true, checkoutUrl, providerSessionId, null, null);

    public static OnlineCheckoutSessionResult Failed(string errorCode, string errorMessage) =>
        new(false, null, null, errorCode, errorMessage);
}
