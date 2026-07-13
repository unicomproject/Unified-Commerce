using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;

public class CheckoutSessionLine : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid CheckoutSessionId { get; protected set; }
    public int LineNumber { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public string? SkuSnapshot { get; protected set; }
    public string ProductNameSnapshot { get; protected set; } = string.Empty;
    public decimal Quantity { get; protected set; }
    public decimal UnitPrice { get; protected set; }
    public decimal LineSubtotalAmount { get; protected set; }
    public decimal LineDiscountAmount { get; protected set; }
    public decimal LineTaxAmount { get; protected set; }
    public decimal LineTotalAmount { get; protected set; }
    public string LineStatus { get; protected set; } = string.Empty;
}

