using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Orders.Entities;

public class SalesOrder : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Status { get; protected set; } = string.Empty; // Maps to order_status
    public string OrderNumber { get; protected set; } = string.Empty;
    public decimal PaidAmount { get; protected set; }
    public decimal TotalAmount { get; protected set; }
    public Guid? DocumentNumberSequenceId { get; protected set; }
    public string? ExternalOrderReference { get; protected set; }
    public Guid SalesChannelId { get; protected set; }
    public string OrderType { get; protected set; } = string.Empty;
    public Guid? FulfillmentMethodOutletId { get; protected set; }
    public string? FulfillmentMethodCodeSnapshot { get; protected set; }
    public Guid? CustomerId { get; protected set; }
    public string? CustomerNameSnapshot { get; protected set; }
    public string? CustomerEmailSnapshot { get; protected set; }
    public string? CustomerPhoneSnapshot { get; protected set; }
    public Guid? TillId { get; protected set; }
    public Guid? TillSessionId { get; protected set; }
    public Guid? PriceListId { get; protected set; }
    public string CurrencyCode { get; protected set; } = string.Empty;
    public decimal SubtotalAmount { get; protected set; }
    public decimal DiscountAmount { get; protected set; }
    public decimal TaxAmount { get; protected set; }
    public decimal ChargeAmount { get; protected set; }
    public decimal RoundingAmount { get; protected set; }
    public decimal RefundedAmount { get; protected set; }
    public decimal BalanceDue { get; protected set; }
    public string PaymentStatus { get; protected set; } = string.Empty;
    public string FulfillmentStatus { get; protected set; } = string.Empty;
    public string? CustomerNote { get; protected set; }
    public string? InternalNote { get; protected set; }
    public DateTimeOffset? PlacedAt { get; protected set; }
    public DateTimeOffset? ConfirmedAt { get; protected set; }
    public DateTimeOffset? CompletedAt { get; protected set; }
    public DateTimeOffset? CancelledAt { get; protected set; }
    public string? CancellationReason { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
}

