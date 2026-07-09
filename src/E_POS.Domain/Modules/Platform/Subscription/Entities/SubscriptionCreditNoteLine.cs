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
}
