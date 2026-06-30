using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CartCheckout.Entities;

public class ShoppingCartItemComponent : AuditableEntity
{
    public decimal Quantity { get; protected set; }
    public Guid ShoppingCartItemId { get; protected set; }
    public int SortOrder { get; protected set; }
}
