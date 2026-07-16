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
    public DateOnly? BusinessDate { get; protected set; }
    public Guid? ReportingOutletId { get; protected set; }
    public string? ReportingOutletCodeSnapshot { get; protected set; }
    public string? ReportingOutletNameSnapshot { get; protected set; }
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

    public static SalesOrder CreateCompletedPosSale(
        Guid id,
        Guid tenantId,
        string orderNumber,
        Guid salesChannelId,
        Guid? customerId,
        string? customerNameSnapshot,
        Guid tillId,
        Guid tillSessionId,
        Guid? priceListId,
        string currencyCode,
        decimal subtotalAmount,
        decimal discountAmount,
        decimal taxAmount,
        decimal totalAmount,
        decimal paidAmount,
        Guid? createdByTenantUserId,
        DateTimeOffset now) =>
        CreateCompletedPosSale(
            id,
            tenantId,
            orderNumber,
            salesChannelId,
            customerId,
            customerNameSnapshot,
            tillId,
            tillSessionId,
            priceListId,
            currencyCode,
            subtotalAmount,
            discountAmount,
            taxAmount,
            totalAmount,
            paidAmount,
            DateOnly.FromDateTime(now.UtcDateTime),
            null,
            null,
            null,
            createdByTenantUserId,
            now);

    public static SalesOrder CreateCompletedPosSale(
        Guid id,
        Guid tenantId,
        string orderNumber,
        Guid salesChannelId,
        Guid? customerId,
        string? customerNameSnapshot,
        Guid tillId,
        Guid tillSessionId,
        Guid? priceListId,
        string currencyCode,
        decimal subtotalAmount,
        decimal discountAmount,
        decimal taxAmount,
        decimal totalAmount,
        decimal paidAmount,
        DateOnly businessDate,
        Guid? reportingOutletId,
        string? reportingOutletCodeSnapshot,
        string? reportingOutletNameSnapshot,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new SalesOrder
        {
            Id = id,
            TenantId = tenantId,
            OrderNumber = orderNumber.Trim(),
            SalesChannelId = salesChannelId,
            OrderType = "POS_SALE",
            BusinessDate = businessDate,
            ReportingOutletId = reportingOutletId,
            ReportingOutletCodeSnapshot = reportingOutletCodeSnapshot?.Trim(),
            ReportingOutletNameSnapshot = reportingOutletNameSnapshot?.Trim(),
            CustomerId = customerId,
            CustomerNameSnapshot = customerNameSnapshot?.Trim(),
            TillId = tillId,
            TillSessionId = tillSessionId,
            PriceListId = priceListId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            SubtotalAmount = subtotalAmount,
            DiscountAmount = discountAmount,
            TaxAmount = taxAmount,
            ChargeAmount = 0,
            RoundingAmount = 0,
            TotalAmount = totalAmount,
            PaidAmount = paidAmount,
            RefundedAmount = 0,
            BalanceDue = 0,
            Status = "COMPLETED",
            PaymentStatus = "PAID",
            FulfillmentStatus = "FULFILLED",
            PlacedAt = now,
            ConfirmedAt = now,
            CompletedAt = now,
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static SalesOrder CreateHeldPosSale(
        Guid id, Guid tenantId, string orderNumber, string idempotencyReference,
        Guid salesChannelId, Guid? customerId, string? customerNameSnapshot,
        Guid tillId, Guid tillSessionId, Guid? priceListId, string currencyCode,
        decimal subtotalAmount, decimal discountAmount, decimal taxAmount,
        decimal totalAmount, string? reason, Guid createdByTenantUserId,
        DateTimeOffset now)
    {
        return new SalesOrder
        {
            Id = id,
            TenantId = tenantId,
            OrderNumber = orderNumber.Trim(),
            ExternalOrderReference = idempotencyReference,
            SalesChannelId = salesChannelId,
            OrderType = "POS_SALE",
            CustomerId = customerId,
            CustomerNameSnapshot = customerNameSnapshot?.Trim(),
            TillId = tillId,
            TillSessionId = tillSessionId,
            PriceListId = priceListId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            SubtotalAmount = subtotalAmount,
            DiscountAmount = discountAmount,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            PaidAmount = 0,
            RefundedAmount = 0,
            BalanceDue = totalAmount,
            Status = "DRAFT",
            PaymentStatus = "UNPAID",
            FulfillmentStatus = "PENDING",
            InternalNote = reason?.Trim(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void RecordRefund(decimal amount, Guid tenantUserId, DateTimeOffset now)
    {
        if (amount <= 0 || RefundedAmount + amount > TotalAmount)
        {
            throw new InvalidOperationException("Refund amount exceeds the order total.");
        }

        RefundedAmount += amount;
        PaymentStatus = RefundedAmount >= TotalAmount ? "REFUNDED" : "PARTIALLY_REFUNDED";
        UpdatedByTenantUserId = tenantUserId;
        UpdatedAt = now;
    }
}

