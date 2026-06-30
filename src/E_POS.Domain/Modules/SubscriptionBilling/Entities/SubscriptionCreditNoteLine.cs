using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class SubscriptionCreditNoteLine : AuditableEntity
{
    public decimal LineCreditAmount { get; protected set; }
    public string LineNumber { get; protected set; } = string.Empty;
    public Guid SubscriptionCreditNoteId { get; protected set; }
}
