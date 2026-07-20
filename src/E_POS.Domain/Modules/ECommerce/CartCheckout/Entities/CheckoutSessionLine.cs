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

    protected CheckoutSessionLine() { }

    public static CheckoutSessionLine CreateFromCartItem(
        Guid id,
        Guid tenantId,
        Guid checkoutSessionId,
        ShoppingCartItem item,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.TenantId != tenantId)
            throw new InvalidOperationException("Cart item tenant does not match checkout tenant.");

        return new CheckoutSessionLine
        {
            Id = id,
            TenantId = tenantId,
            CheckoutSessionId = checkoutSessionId,
            LineNumber = item.LineNumber,
            ProductId = item.ProductId,
            ProductVariantId = item.ProductVariantId,
            SkuSnapshot = item.SkuSnapshot,
            ProductNameSnapshot = item.ProductNameSnapshot,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            LineSubtotalAmount = item.LineSubtotalAmount,
            LineDiscountAmount = item.LineDiscountAmount,
            LineTaxAmount = item.LineTaxAmount,
            LineTotalAmount = item.LineTotalAmount,
            LineStatus = "ACTIVE",
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

