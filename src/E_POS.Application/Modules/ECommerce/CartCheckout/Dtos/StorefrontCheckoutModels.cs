namespace E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;

public sealed class CreateStorefrontCheckoutFromCartRequest
{
    public Guid SelectedOutletId { get; set; }
    public Guid? SelectedPickupSlotId { get; set; }
    public string? PickupContactName { get; set; }
    public string? PickupContactPhone { get; set; }
    public string? PickupContactEmail { get; set; }
}

public sealed class StorefrontCheckoutReadModel
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public string CheckoutNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FulfillmentMethodCode { get; set; } = string.Empty;
    public Guid SelectedOutletId { get; set; }
    public string SelectedOutletName { get; set; } = string.Empty;
    public Guid? SelectedPickupSlotId { get; set; }
    public StorefrontCheckoutPickupSlotReadModel? PickupSlot { get; set; }
    public string? PickupContactName { get; set; }
    public string? PickupContactPhone { get; set; }
    public string? PickupContactEmail { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal ChargeTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal TotalQuantity { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public IReadOnlyList<StorefrontCheckoutLineReadModel> Items { get; set; } = [];
    public StorefrontCheckoutOrderReadModel? Order { get; set; }
}

public sealed class StorefrontCheckoutLineReadModel
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string? Sku { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class StorefrontCheckoutPickupSlotReadModel
{
    public string SlotCode { get; set; } = string.Empty;
    public DateOnly SlotDate { get; set; }
    public TimeOnly WindowStart { get; set; }
    public TimeOnly WindowEnd { get; set; }
}

public sealed class StorefrontCheckoutOrderReadModel
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string FulfillmentStatus { get; set; } = string.Empty;
}
