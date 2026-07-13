using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;

public class CheckoutSession : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? SalesChannelId { get; protected set; }
    public Guid CartId { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public string? AnonymousSessionId { get; protected set; }
    public string CheckoutNumber { get; protected set; } = string.Empty;
    public string CheckoutStatus { get; protected set; } = string.Empty;
    public string SalesChannel { get; protected set; } = string.Empty;
    public string? FulfillmentMethodCode { get; protected set; }
    public Guid? SelectedOutletId { get; protected set; }
    public Guid? SelectedPickupSlotId { get; protected set; }
    public string? PickupContactName { get; protected set; }
    public string? PickupContactPhone { get; protected set; }
    public string? PickupContactEmail { get; protected set; }
    public string CurrencyCode { get; protected set; } = string.Empty;
    public decimal SubtotalAmount { get; protected set; }
    public decimal DiscountAmount { get; protected set; }
    public decimal TaxAmount { get; protected set; }
    public decimal ChargeAmount { get; protected set; }
    public decimal TotalAmount { get; protected set; }
    public Guid? InventoryReservationId { get; protected set; }
    public Guid? ConvertedOrderId { get; protected set; }
    public DateTimeOffset? CompletedAt { get; protected set; }
    public DateTimeOffset? ExpiredAt { get; protected set; }
}

