using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CartCheckout.Entities;

public class CheckoutSessionLineComponent : AuditableEntity
{
    public Guid CheckoutSessionLineId { get; protected set; }
    public decimal Quantity { get; protected set; }
    public int SortOrder { get; protected set; }
}
