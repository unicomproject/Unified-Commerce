using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionCreditNote : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string CreditNoteNumber { get; protected set; } = string.Empty;
    public Guid SubscriptionInvoiceId { get; protected set; }
    public decimal TotalCreditAmount { get; protected set; }
    public Guid InvoiceId { get; protected set; }
    public string? Reason { get; protected set; }
    public string CurrencyCode { get; protected set; } = "LKR";
    public decimal SubtotalAmount { get; protected set; }
    public decimal TaxAmount { get; protected set; }
    public decimal TotalAmount { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public DateTimeOffset? IssuedAt { get; protected set; }
    public DateTimeOffset? AppliedAt { get; protected set; }
    public Guid? CreatedByPlatformUserId { get; protected set; }

    public static SubscriptionCreditNote CreateDraft(
        Guid id,
        Guid tenantId,
        Guid subscriptionInvoiceId,
        string creditNoteNumber,
        decimal totalCreditAmount,
        string currencyCode,
        DateTimeOffset now,
        string? reason = null,
        decimal? taxAmount = null,
        Guid? createdByPlatformUserId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(creditNoteNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);

        var normalizedTotal = Math.Max(0m, totalCreditAmount);
        var normalizedTax = Math.Max(0m, taxAmount ?? 0m);
        var normalizedSubtotal = Math.Max(0m, normalizedTotal - normalizedTax);

        return new SubscriptionCreditNote
        {
            Id = id,
            TenantId = tenantId,
            SubscriptionInvoiceId = subscriptionInvoiceId,
            InvoiceId = subscriptionInvoiceId,
            CreditNoteNumber = creditNoteNumber.Trim(),
            TotalCreditAmount = normalizedTotal,
            TotalAmount = normalizedTotal,
            SubtotalAmount = normalizedSubtotal,
            TaxAmount = normalizedTax,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Reason = NormalizeOptional(reason),
            Status = SubscriptionBillingAlignmentConstants.CreditNoteStatusDraft,
            CreatedByPlatformUserId = createdByPlatformUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Issue(DateTimeOffset now)
    {
        Status = SubscriptionBillingAlignmentConstants.CreditNoteStatusIssued;
        IssuedAt = now;
        UpdatedAt = now;
    }

    public void Apply(DateTimeOffset now)
    {
        Status = SubscriptionBillingAlignmentConstants.CreditNoteStatusApplied;
        AppliedAt = now;
        UpdatedAt = now;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
