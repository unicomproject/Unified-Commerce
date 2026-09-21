namespace E_POS.Application.Modules.ECommerce.CartCheckout.Payment.Contracts;

/// <summary>
/// Durable, provider-level deduplication for inbound payment webhooks. Providers deliver events
/// at-least-once, so the same event id can arrive more than once — sometimes so close together
/// that both deliveries would otherwise pass any payment-status check before either commits. The
/// unique record inserted here is the actual tie-breaker, independent of business state.
/// </summary>
public interface IPaymentWebhookEventDeduplicator
{
    /// <returns>
    /// <c>true</c> the first time this (provider, externalEventId) pair is seen — the caller
    /// should process it. <c>false</c> if it has already been recorded — the caller should skip
    /// processing and simply acknowledge receipt.
    /// </returns>
    Task<bool> TryRecordAsync(
        string provider,
        string externalEventId,
        string eventType,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
