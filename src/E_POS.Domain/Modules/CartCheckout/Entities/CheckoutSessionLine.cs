using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CartCheckout.Entities;

public class CheckoutSessionLine : AuditableEntity
{
    public Guid? CheckoutSessionId { get; protected set; }
    public string LineNumber { get; protected set; } = string.Empty;
    public decimal Quantity { get; protected set; }
}
