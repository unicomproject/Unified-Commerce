using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;

public class ShoppingCartItem : AuditableEntity
{
    public string LineNumber { get; protected set; } = string.Empty;
    public decimal Quantity { get; protected set; }
    public Guid ShoppingCartId { get; protected set; }
}

