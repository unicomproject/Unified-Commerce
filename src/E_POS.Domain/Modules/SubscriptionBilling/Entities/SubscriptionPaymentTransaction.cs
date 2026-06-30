using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class SubscriptionPaymentTransaction : AuditableEntity
{
    public decimal Amount { get; protected set; }
    public string ProviderTransactionReference { get; protected set; } = string.Empty;
    public Guid SubscriptionInvoiceId { get; protected set; }
    public Guid SubscriptionPaymentLinkId { get; protected set; }
}
