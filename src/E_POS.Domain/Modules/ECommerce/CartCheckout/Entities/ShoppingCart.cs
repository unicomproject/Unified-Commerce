using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.CartCheckout.Entities;

public class ShoppingCart : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? SalesChannelId { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public string? AnonymousSessionId { get; protected set; }
    public string CartNumber { get; protected set; } = string.Empty;
    public string SalesChannel { get; protected set; } = string.Empty;
    public string CurrencyCode { get; protected set; } = string.Empty;
    public string CartStatus { get; protected set; } = string.Empty;
    public decimal SubtotalAmount { get; protected set; }
    public decimal DiscountAmount { get; protected set; }
    public decimal TaxAmount { get; protected set; }
    public decimal ChargeAmount { get; protected set; }
    public decimal TotalAmount { get; protected set; }
    public DateTimeOffset? ExpiresAt { get; protected set; }
    public Guid? ConvertedCheckoutSessionId { get; protected set; }
    public Guid? ConvertedOrderId { get; protected set; }
}

