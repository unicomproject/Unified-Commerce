namespace E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;

/// <summary>
/// Records that a payment provider webhook event (e.g. a Stripe event id) has been received,
/// so a duplicate delivery of the same event can be recognised and skipped before any business
/// logic runs. Providers routinely redeliver the same event (retries, at-least-once delivery),
/// and payment-status checks alone cannot close that race: a duplicate can arrive again after
/// the original was already fully processed, or so close behind it that both see the same
/// pre-transition state. The unique index on (Provider, ExternalEventId) is the actual guard —
/// insertion failing with a uniqueness violation is what "duplicate" means here, not a prior read.
/// </summary>
public class PaymentProviderWebhookEvent
{
    public Guid Id { get; protected set; }
    public string Provider { get; protected set; } = string.Empty;
    public string ExternalEventId { get; protected set; } = string.Empty;
    public string EventType { get; protected set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; protected set; }

    public static PaymentProviderWebhookEvent Create(
        Guid id,
        string provider,
        string externalEventId,
        string eventType,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(externalEventId))
            throw new ArgumentException("External event id is required.", nameof(externalEventId));

        return new PaymentProviderWebhookEvent
        {
            Id = id,
            Provider = provider.Trim().ToUpperInvariant(),
            ExternalEventId = externalEventId.Trim(),
            EventType = string.IsNullOrWhiteSpace(eventType) ? string.Empty : eventType.Trim(),
            ReceivedAt = now
        };
    }
}
