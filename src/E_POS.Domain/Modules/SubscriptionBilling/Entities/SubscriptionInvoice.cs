using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class SubscriptionInvoice : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string InvoiceNumber { get; protected set; } = string.Empty;
    public Guid TenantSubscriptionId { get; protected set; }
    public decimal TotalAmount { get; protected set; }
}
