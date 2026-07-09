using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionCreditNoteLine : AuditableEntity
{
    public decimal LineCreditAmount { get; protected set; }
    public string LineNumber { get; protected set; } = string.Empty;
    public Guid SubscriptionCreditNoteId { get; protected set; }
    public Guid CreditNoteId { get; protected set; }
    public Guid? InvoiceLineId { get; protected set; }
    public int? LineNumberInt { get; protected set; }
    public string Description { get; protected set; } = string.Empty;
    public decimal Quantity { get; protected set; }
    public decimal UnitAmount { get; protected set; }
    public decimal TaxAmount { get; protected set; }
    public decimal LineTotal { get; protected set; }

    public static SubscriptionCreditNoteLine Create(
        Guid id,
        Guid subscriptionCreditNoteId,
        string lineNumber,
        int? lineNumberInt,
        string description,
        decimal quantity,
        decimal unitAmount,
        decimal lineCreditAmount,
        DateTimeOffset now,
        Guid? invoiceLineId = null,
        decimal? taxAmount = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lineNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var normalizedLineCredit = Math.Max(0m, lineCreditAmount);
        var normalizedQuantity = Math.Max(0m, quantity);
        var normalizedUnitAmount = Math.Max(0m, unitAmount);
        var normalizedTax = Math.Max(0m, taxAmount ?? 0m);

        return new SubscriptionCreditNoteLine
        {
            Id = id,
            SubscriptionCreditNoteId = subscriptionCreditNoteId,
            CreditNoteId = subscriptionCreditNoteId,
            LineNumber = lineNumber.Trim(),
            LineNumberInt = lineNumberInt,
            Description = description.Trim(),
            Quantity = normalizedQuantity,
            UnitAmount = normalizedUnitAmount,
            LineCreditAmount = normalizedLineCredit,
            LineTotal = normalizedLineCredit,
            TaxAmount = normalizedTax,
            InvoiceLineId = invoiceLineId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
