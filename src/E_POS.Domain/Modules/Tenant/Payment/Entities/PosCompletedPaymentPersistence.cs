namespace E_POS.Domain.Modules.Tenant.Payment.Entities;

/// <summary>
/// Maps a successful POS payment (cash or provider) into sales payment aggregates.
/// Card brand/last4 are stored only as sanitized metadata on the transaction row.
/// </summary>
public static class PosCompletedPaymentPersistence
{
    public sealed record ProviderCaptureOutcome(
        string ProviderName,
        string? ProviderTransactionId,
        string? CardBrand,
        string? CardLast4);

    public sealed record CompletedPaymentRecords(
        SalesPayment Payment,
        SalesPaymentTransaction Transaction,
        SalesPaymentEvent Event);

    public static CompletedPaymentRecords CreateCash(
        Guid paymentId,
        Guid tenantId,
        Guid salesOrderId,
        string paymentNumber,
        Guid paymentMethodId,
        Guid tillId,
        Guid tillSessionId,
        string currencyCode,
        decimal requestedAmount,
        decimal? tenderedAmount,
        decimal paidAmount,
        decimal changeAmount,
        string idempotencyKey,
        string requestHash,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        var payment = SalesPayment.CreateCompletedPosPayment(
            paymentId,
            tenantId,
            salesOrderId,
            paymentNumber,
            paymentMethodId,
            tillId,
            tillSessionId,
            currencyCode,
            requestedAmount,
            tenderedAmount,
            paidAmount,
            changeAmount,
            idempotencyKey,
            requestHash,
            createdByTenantUserId,
            now);

        var transaction = SalesPaymentTransaction.CreateCompletedCash(
            Guid.NewGuid(),
            tenantId,
            paymentId,
            paidAmount,
            currencyCode,
            idempotencyKey,
            createdByTenantUserId,
            now);

        var paymentEvent = SalesPaymentEvent.RecordPaid(
            Guid.NewGuid(),
            tenantId,
            paymentId,
            createdByTenantUserId,
            now);

        return new CompletedPaymentRecords(payment, transaction, paymentEvent);
    }

    /// <summary>
    /// Persist a successful provider/terminal capture. Call only after authorization succeeded.
    /// Does not fabricate references; omits card tips when brand/last4 are absent or invalid.
    /// </summary>
    public static CompletedPaymentRecords CreateProviderCapture(
        Guid paymentId,
        Guid tenantId,
        Guid salesOrderId,
        string paymentNumber,
        Guid paymentMethodId,
        Guid tillId,
        Guid tillSessionId,
        string currencyCode,
        decimal requestedAmount,
        decimal paidAmount,
        string idempotencyKey,
        string requestHash,
        Guid? createdByTenantUserId,
        DateTimeOffset now,
        ProviderCaptureOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        if (string.IsNullOrWhiteSpace(outcome.ProviderName))
        {
            throw new ArgumentException("Provider name is required for card capture.", nameof(outcome));
        }

        var payment = SalesPayment.CreateCompletedPosPayment(
            paymentId,
            tenantId,
            salesOrderId,
            paymentNumber,
            paymentMethodId,
            tillId,
            tillSessionId,
            currencyCode,
            requestedAmount,
            tenderedAmount: null,
            paidAmount,
            changeAmount: 0m,
            idempotencyKey,
            requestHash,
            createdByTenantUserId,
            now,
            externalReference: outcome.ProviderTransactionId);

        var transaction = SalesPaymentTransaction.CreateCompletedProviderCapture(
            Guid.NewGuid(),
            tenantId,
            paymentId,
            paidAmount,
            currencyCode,
            outcome.ProviderName,
            outcome.ProviderTransactionId,
            outcome.CardBrand,
            outcome.CardLast4,
            idempotencyKey,
            createdByTenantUserId,
            now);

        var paymentEvent = SalesPaymentEvent.RecordPaid(
            Guid.NewGuid(),
            tenantId,
            paymentId,
            createdByTenantUserId,
            now,
            eventNote: "Provider payment completed at POS.");

        return new CompletedPaymentRecords(payment, transaction, paymentEvent);
    }
}
