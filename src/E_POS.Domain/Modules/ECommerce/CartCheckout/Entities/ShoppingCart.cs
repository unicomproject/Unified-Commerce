using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;

public class ShoppingCart : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string CartStatus { get; protected set; } = string.Empty;
    public string CartNumber { get; protected set; } = string.Empty;
}

