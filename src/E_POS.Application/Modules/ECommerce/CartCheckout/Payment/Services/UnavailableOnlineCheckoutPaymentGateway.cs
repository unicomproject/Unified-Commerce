using E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Contracts;

namespace E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Services;

public sealed class UnavailableOnlineCheckoutPaymentGateway : IOnlineCheckoutPaymentGateway
{
    public static UnavailableOnlineCheckoutPaymentGateway Instance { get; } = new();

    private UnavailableOnlineCheckoutPaymentGateway()
    {
    }

    public Task<OnlineCheckoutSessionResult> CreateCheckoutSessionAsync(
        OnlineCheckoutSessionRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(OnlineCheckoutSessionResult.Failed(
            "online_payment_gateway_unavailable",
            "Online payment is not configured."));
}
