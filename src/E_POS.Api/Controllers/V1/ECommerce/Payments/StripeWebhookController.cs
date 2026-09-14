using E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Contracts;
using E_POS.Infrastructure.Modules.ECommerce.CartCheckout.Payment;
using E_POS.Infrastructure.Modules.Shared.Payment.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace E_POS.Api.Controllers.V1.ECommerce.Payments;

[ApiController]
[Route("api/v1/ecommerce/payments/stripe/webhook")]
public sealed class StripeWebhookController : ControllerBase
{
    private readonly StripeOptions _options;
    private readonly IOnlineCheckoutPaymentConfirmationService _confirmationService;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IOptions<StripeOptions> options,
        IOnlineCheckoutPaymentConfirmationService confirmationService,
        ILogger<StripeWebhookController> logger)
    {
        _options = options.Value;
        _confirmationService = confirmationService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"].ToString(),
                _options.WebhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed.");
            return BadRequest();
        }

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutSessionCompletedAsync(stripeEvent, cancellationToken);
                break;
            case "checkout.session.expired":
                await HandleCheckoutSessionExpiredAsync(stripeEvent, "Stripe checkout session expired.", cancellationToken);
                break;
        }

        return Ok();
    }

    private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Session session) return;
        if (!TryReadMetadata(session.Metadata, out var tenantId, out var salesOrderId, out var salesPaymentId)) return;

        var currencyCode = (session.Currency ?? string.Empty).ToUpperInvariant();
        var paidAmount = StripeMoney.FromMinorUnits(session.AmountTotal ?? 0, currencyCode);

        await _confirmationService.HandleCheckoutCompletedAsync(
            tenantId,
            salesOrderId,
            salesPaymentId,
            paidAmount,
            session.PaymentIntentId,
            null,
            cancellationToken);
    }

    private async Task HandleCheckoutSessionExpiredAsync(Event stripeEvent, string reason, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Session session) return;
        if (!TryReadMetadata(session.Metadata, out var tenantId, out var salesOrderId, out var salesPaymentId)) return;

        await _confirmationService.HandleCheckoutExpiredAsync(
            tenantId, salesOrderId, salesPaymentId, reason, cancellationToken);
    }

    private static bool TryReadMetadata(
        IDictionary<string, string>? metadata,
        out Guid tenantId,
        out Guid salesOrderId,
        out Guid salesPaymentId)
    {
        tenantId = Guid.Empty;
        salesOrderId = Guid.Empty;
        salesPaymentId = Guid.Empty;
        if (metadata is null) return false;

        return metadata.TryGetValue("tenantId", out var tenantIdValue) && Guid.TryParse(tenantIdValue, out tenantId) &&
               metadata.TryGetValue("salesOrderId", out var orderIdValue) && Guid.TryParse(orderIdValue, out salesOrderId) &&
               metadata.TryGetValue("salesPaymentId", out var paymentIdValue) && Guid.TryParse(paymentIdValue, out salesPaymentId);
    }
}
