using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionPaymentLink : AuditableEntity
{
    public DateTimeOffset ExpiresAt { get; protected set; }
    public string PaymentLinkTokenHash { get; protected set; } = string.Empty;
    public Guid SubscriptionInvoiceId { get; protected set; }
}

