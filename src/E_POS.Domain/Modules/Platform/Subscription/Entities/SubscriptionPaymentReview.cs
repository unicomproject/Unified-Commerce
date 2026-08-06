using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public sealed class SubscriptionPaymentReview : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid PaymentId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string StatusBefore { get; private set; } = string.Empty;
    public string StatusAfter { get; private set; } = string.Empty;
    public string ActorType { get; private set; } = string.Empty;
    public Guid? ActorId { get; private set; }
    public string? ReviewNote { get; private set; }
    public string? ReasonCode { get; private set; }
    public string IdempotencyKeyHash { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public Guid CorrelationId { get; private set; }
    public long PaymentVersion { get; private set; }
    public decimal? SubmittedAmountSnapshot { get; private set; }
    public decimal ExpectedAmountSnapshot { get; private set; }
    public string CurrencySnapshot { get; private set; } = string.Empty;
    public Guid? EvidenceIdSnapshot { get; private set; }
    public long? EvidenceVersionSnapshot { get; private set; }

    public static SubscriptionPaymentReview Create(Guid id, Guid tenantId, Guid paymentId, Guid invoiceId,
        string action, string before, string after, string actorType, Guid? actorId,
        string? note, string? reasonCode, string idempotencyKeyHash, string requestHash,
        Guid correlationId, long paymentVersion, DateTimeOffset now,
        decimal? submittedAmountSnapshot = null, decimal expectedAmountSnapshot = 0,
        string? currencySnapshot = null, Guid? evidenceIdSnapshot = null,
        long? evidenceVersionSnapshot = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(before);
        ArgumentException.ThrowIfNullOrWhiteSpace(after);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorType);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKeyHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);
        return new SubscriptionPaymentReview
        {
            Id = id,
            TenantId = tenantId,
            PaymentId = paymentId,
            InvoiceId = invoiceId,
            Action = action.Trim().ToUpperInvariant(),
            StatusBefore = before.Trim().ToUpperInvariant(),
            StatusAfter = after.Trim().ToUpperInvariant(),
            ActorType = actorType.Trim().ToUpperInvariant(),
            ActorId = actorId,
            ReviewNote = Normalize(note),
            ReasonCode = Normalize(reasonCode)?.ToUpperInvariant(),
            IdempotencyKeyHash = idempotencyKeyHash.Trim(),
            RequestHash = requestHash.Trim(),
            CorrelationId = correlationId,
            PaymentVersion = paymentVersion,
            SubmittedAmountSnapshot = submittedAmountSnapshot,
            ExpectedAmountSnapshot = expectedAmountSnapshot,
            CurrencySnapshot = currencySnapshot?.Trim().ToUpperInvariant() ?? string.Empty,
            EvidenceIdSnapshot = evidenceIdSnapshot,
            EvidenceVersionSnapshot = evidenceVersionSnapshot,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
