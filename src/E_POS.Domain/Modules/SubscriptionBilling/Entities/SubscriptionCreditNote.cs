using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class SubscriptionCreditNote : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string CreditNoteNumber { get; protected set; } = string.Empty;
    public Guid SubscriptionInvoiceId { get; protected set; }
    public decimal TotalCreditAmount { get; protected set; }
}
