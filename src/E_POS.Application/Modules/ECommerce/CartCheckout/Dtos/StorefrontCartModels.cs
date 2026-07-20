namespace E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;

public sealed class AddStorefrontCartItemRequest
{
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public decimal Quantity { get; set; } = 1m;
}

public sealed class UpdateStorefrontCartItemRequest
{
    public decimal Quantity { get; set; }
}

public sealed class StorefrontCartReadModel
{
    public Guid Id { get; set; }
    public string CartNumber { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<StorefrontCartItemReadModel> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal ChargeTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal TotalQuantity { get; set; }
    public bool IsTaxInclusive { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public sealed class StorefrontCartItemReadModel
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string? Slug { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? Sku { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsInStock { get; set; }
    public IReadOnlyList<StorefrontCartItemOptionReadModel> Options { get; set; } = [];
}

public sealed class StorefrontCartItemOptionReadModel
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
}
