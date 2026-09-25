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
    private const string Provider = "STRIPE";

    private readonly StripeOptions _options;
    private readonly IOnlineCheckoutPaymentConfirmationService _confirmationService;
    private readonly IPaymentWebhookEventDeduplicator _deduplicator;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IOptions<StripeOptions> options,
        IOnlineCheckoutPaymentConfirmationService confirmationService,
        IPaymentWebhookEventDeduplicator deduplicator,
        ILogger<StripeWebhookController> logger)
    {
        _options = options.Value;
        _confirmationService = confirmationService;
        _deduplicator = deduplicator;
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

        // Stripe delivers at-least-once: the same event id can arrive again on retry, or two
        // deliveries can race each other closely enough that a payment-status check alone would
        // not catch the duplicate. This durable record is the actual tie-breaker.
        var isNewEvent = await _deduplicator.TryRecordAsync(
            Provider, stripeEvent.Id, stripeEvent.Type, DateTimeOffset.UtcNow, cancellationToken);
        if (!isNewEvent)
        {
            _logger.LogInformation("Ignoring duplicate Stripe webhook delivery for event {StripeEventId}.", stripeEvent.Id);
            return Ok();
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

        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Ignoring checkout.session.completed for Stripe session {StripeSessionId} with payment status {PaymentStatus} — not paid.",
                session.Id, session.PaymentStatus);
            return;
        }

        var currencyCode = (session.Currency ?? string.Empty).ToUpperInvariant();
        var paidAmount = StripeMoney.FromMinorUnits(session.AmountTotal ?? 0, currencyCode);

        await _confirmationService.HandleCheckoutCompletedAsync(
            tenantId,
            salesOrderId,
            salesPaymentId,
            paidAmount,
            currencyCode,
            session.Id,
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
