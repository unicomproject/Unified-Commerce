using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CartCheckout.Entities;

public class CheckoutSession : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string CheckoutNumber { get; protected set; } = string.Empty;
    public Guid ShoppingCartId { get; protected set; }
    public decimal TotalAmount { get; protected set; }
}
