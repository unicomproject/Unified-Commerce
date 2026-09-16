using E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Contracts;
using E_POS.Infrastructure.Modules.Shared.Payment.Options;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Payment;

public sealed class StripeCheckoutGateway : IOnlineCheckoutPaymentGateway
{
    private readonly StripeOptions _options;
    private readonly SessionService _sessionService;

    public StripeCheckoutGateway(IOptions<StripeOptions> options)
        : this(options, new SessionService())
    {
    }

    internal StripeCheckoutGateway(IOptions<StripeOptions> options, SessionService sessionService)
    {
        _options = options.Value;
        _sessionService = sessionService;
    }

    public async Task<OnlineCheckoutSessionResult> CreateCheckoutSessionAsync(
        OnlineCheckoutSessionRequest request,
        CancellationToken cancellationToken)
    {
        var successUrl = AppendCheckoutId(_options.SuccessUrl, request.CheckoutSessionId);
        var cancelUrl = AppendCheckoutId(_options.CancelUrl, request.CheckoutSessionId);

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            // Explicit rather than left to Stripe's automatic detection, which requires
            // "automatic payment methods" to be turned on for the account in the Stripe
            // dashboard and otherwise rejects the session with no valid payment method types.
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.CurrencyCode.ToLowerInvariant(),
                        UnitAmount = StripeMoney.ToMinorUnits(request.Amount, request.CurrencyCode),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Order {request.OrderNumber}"
                        }
                    }
                }
            ],
            CustomerEmail = string.IsNullOrWhiteSpace(request.CustomerEmail) ? null : request.CustomerEmail,
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["tenantId"] = request.TenantId.ToString("N"),
                ["salesOrderId"] = request.SalesOrderId.ToString("N"),
                ["salesPaymentId"] = request.SalesPaymentId.ToString("N")
            }
        };

        try
        {
            var session = await _sessionService.CreateAsync(
                sessionOptions,
                new RequestOptions { ApiKey = _options.SecretKey },
                cancellationToken);
            return OnlineCheckoutSessionResult.Succeeded(session.Url, session.Id);
        }
        catch (StripeException ex)
        {
            return OnlineCheckoutSessionResult.Failed(ex.StripeError?.Code ?? "stripe_error", ex.Message);
        }
    }

    private static string AppendCheckoutId(string baseUrl, Guid checkoutSessionId)
    {
        var separator = baseUrl.Contains('?') ? '&' : '?';
        return $"{baseUrl}{separator}checkoutId={checkoutSessionId:N}";
    }
}
