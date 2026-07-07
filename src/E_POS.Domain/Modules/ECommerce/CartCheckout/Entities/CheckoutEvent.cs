using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;

public class CheckoutEvent : AuditableEntity
{
    public Guid? CheckoutSessionId { get; protected set; }
    public int SequenceNumber { get; protected set; }
}

