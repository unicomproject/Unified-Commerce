using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;

public class ShoppingCartItem : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ShoppingCartId { get; protected set; }
    public int LineNumber { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public string? SkuSnapshot { get; protected set; }
    public string ProductNameSnapshot { get; protected set; } = string.Empty;
    public string? ProductStructure { get; protected set; }
    public decimal Quantity { get; protected set; }
    public decimal UnitPrice { get; protected set; }
    public decimal LineSubtotalAmount { get; protected set; }
    public decimal LineDiscountAmount { get; protected set; }
    public decimal LineTaxAmount { get; protected set; }
    public decimal LineTotalAmount { get; protected set; }
    public string LineStatus { get; protected set; } = string.Empty;

    protected ShoppingCartItem() { }

    public static ShoppingCartItem Create(
        Guid tenantId,
        Guid cartId,
        int lineNumber,
        Guid productId,
        Guid? productVariantId,
        string? sku,
        string productName,
        string? productStructure,
        decimal quantity,
        decimal unitPrice,
        decimal taxPercent,
        DateTimeOffset now)
    {
        var item = new ShoppingCartItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ShoppingCartId = cartId,
            LineNumber = lineNumber,
            ProductId = productId,
            ProductVariantId = productVariantId,
            SkuSnapshot = sku?.Trim(),
            ProductNameSnapshot = productName.Trim(),
            ProductStructure = productStructure?.Trim().ToUpperInvariant(),
            LineStatus = "ACTIVE",
            CreatedAt = now,
            UpdatedAt = now
        };
        item.UpdateQuantityAndPrice(quantity, unitPrice, taxPercent, now);
        return item;
    }

    public void UpdateQuantityAndPrice(decimal quantity, decimal unitPrice, decimal taxPercent, DateTimeOffset now)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));
        Quantity = quantity;
        UnitPrice = decimal.Round(unitPrice, 4, MidpointRounding.AwayFromZero);
        LineSubtotalAmount = decimal.Round(Quantity * UnitPrice, 4, MidpointRounding.AwayFromZero);
        LineDiscountAmount = 0m;
        LineTaxAmount = decimal.Round(LineSubtotalAmount * taxPercent / 100m, 4, MidpointRounding.AwayFromZero);
        LineTotalAmount = LineSubtotalAmount - LineDiscountAmount + LineTaxAmount;
        LineStatus = "ACTIVE";
        UpdatedAt = now;
    }

    public void Remove(DateTimeOffset now)
    {
        LineStatus = "REMOVED";
        UpdatedAt = now;
    }
}

