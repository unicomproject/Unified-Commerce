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
    public DateTimeOffset? RequestedCollectionAt { get; protected set; }
    public DateTimeOffset? RequestedCollectionEndAt { get; protected set; }
    public string? CollectionTimezoneSnapshot { get; protected set; }
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
    public bool IsTaxInclusive { get; protected set; }
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
        bool isTaxInclusive,
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
            isTaxInclusive,
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
        bool isTaxInclusive,
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
        return CreateCompletedOrder(
            id,
            tenantId,
            orderNumber,
            "POS_SALE",
            salesChannelId,
            customerId,
            customerNameSnapshot,
            tillId,
            tillSessionId,
            priceListId,
            currencyCode,
            isTaxInclusive,
            subtotalAmount,
            discountAmount,
            taxAmount,
            totalAmount,
            paidAmount,
            businessDate,
            reportingOutletId,
            reportingOutletCodeSnapshot,
            reportingOutletNameSnapshot,
            createdByTenantUserId,
            now);
    }

    public static SalesOrder CreateCompletedExchangeOrder(
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
        DateTimeOffset now)
    {
        return CreateCompletedOrder(
            id,
            tenantId,
            orderNumber,
            "EXCHANGE_ORDER",
            salesChannelId,
            customerId,
            customerNameSnapshot,
            tillId,
            tillSessionId,
            priceListId,
            currencyCode,
            false, // Exchange orders don't explicitly pass this yet, default to false or whatever is appropriate. Wait, let me just add it to CreateCompletedExchangeOrder as well.
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
    }

    private static SalesOrder CreateCompletedOrder(
        Guid id,
        Guid tenantId,
        string orderNumber,
        string orderType,
        Guid salesChannelId,
        Guid? customerId,
        string? customerNameSnapshot,
        Guid tillId,
        Guid tillSessionId,
        Guid? priceListId,
        string currencyCode,
        bool isTaxInclusive,
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
            OrderType = orderType,
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
            IsTaxInclusive = isTaxInclusive,
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
        bool isTaxInclusive,
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
            IsTaxInclusive = isTaxInclusive,
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

    public static SalesOrder CreateClickAndCollect(
        Guid id,
        Guid tenantId,
        string orderNumber,
        string idempotencyReference,
        Guid salesChannelId,
        Guid? fulfillmentMethodOutletId,
        string fulfillmentMethodCode,
        Guid outletId,
        string outletCode,
        string outletName,
        Guid customerId,
        string customerName,
        string? customerEmail,
        string? customerPhone,
        string currencyCode,
        bool isTaxInclusive,
        decimal subtotalAmount,
        decimal discountAmount,
        decimal taxAmount,
        decimal chargeAmount,
        decimal totalAmount,
        DateTimeOffset requestedCollectionAt,
        DateTimeOffset requestedCollectionEndAt,
        string collectionTimezone,
        DateTimeOffset now)
    {
        return new SalesOrder
        {
            Id = id,
            TenantId = tenantId,
            OrderNumber = orderNumber.Trim().ToUpperInvariant(),
            ExternalOrderReference = idempotencyReference.Trim(),
            SalesChannelId = salesChannelId,
            OrderType = "CLICK_AND_COLLECT",
            FulfillmentMethodOutletId = fulfillmentMethodOutletId,
            FulfillmentMethodCodeSnapshot = fulfillmentMethodCode.Trim().ToUpperInvariant(),
            RequestedCollectionAt = requestedCollectionAt,
            RequestedCollectionEndAt = requestedCollectionEndAt,
            CollectionTimezoneSnapshot = collectionTimezone.Trim(),
            BusinessDate = DateOnly.FromDateTime(now.UtcDateTime),
            ReportingOutletId = outletId,
            ReportingOutletCodeSnapshot = outletCode.Trim(),
            ReportingOutletNameSnapshot = outletName.Trim(),
            CustomerId = customerId,
            CustomerNameSnapshot = customerName.Trim(),
            CustomerEmailSnapshot = string.IsNullOrWhiteSpace(customerEmail) ? null : customerEmail.Trim(),
            CustomerPhoneSnapshot = string.IsNullOrWhiteSpace(customerPhone) ? null : customerPhone.Trim(),
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            IsTaxInclusive = isTaxInclusive,
            SubtotalAmount = subtotalAmount,
            DiscountAmount = discountAmount,
            TaxAmount = taxAmount,
            ChargeAmount = chargeAmount,
            RoundingAmount = 0m,
            TotalAmount = totalAmount,
            PaidAmount = 0m,
            RefundedAmount = 0m,
            BalanceDue = totalAmount,
            Status = "CONFIRMED",
            PaymentStatus = "UNPAID",
            FulfillmentStatus = "PENDING",
            PlacedAt = now,
            ConfirmedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }


    public void UpdateClickAndCollectStatus(
        string targetStatus,
        Guid updatedByTenantUserId,
        DateTimeOffset now)
    {
        if (!string.Equals(OrderType, "CLICK_AND_COLLECT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only click and collect orders can use this status workflow.");

        var normalizedTarget = targetStatus.Trim().Replace('-', '_').ToUpperInvariant();
        var currentStatus = GetClickAndCollectCustomerStatus();
        if (!IsAllowedClickAndCollectTransition(currentStatus, normalizedTarget))
        {
            throw new InvalidOperationException(
                $"Cannot change click and collect order status from {currentStatus} to {normalizedTarget}.");
        }

        switch (normalizedTarget)
        {
            case "ACCEPTED":
                Status = "ACCEPTED";
                FulfillmentStatus = "ACCEPTED";
                ConfirmedAt ??= now;
                break;
            case "PREPARING":
                Status = "ACCEPTED";
                FulfillmentStatus = "PREPARING";
                break;
            case "READY_FOR_COLLECTION":
                Status = "ACCEPTED";
                FulfillmentStatus = "READY_FOR_COLLECTION";
                break;
            case "COMPLETED":
                Status = "COMPLETED";
                FulfillmentStatus = "COLLECTED";
                CompletedAt = now;
                break;
            default:
                throw new InvalidOperationException("Unsupported click and collect target status.");
        }

        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void CancelClickAndCollectByCustomer(
        string? reason,
        DateTimeOffset now)
    {
        if (!string.Equals(OrderType, "CLICK_AND_COLLECT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only click and collect orders can be cancelled by a customer.");

        var currentStatus = GetClickAndCollectCustomerStatus();
        if (currentStatus is not "PENDING_CONFIRMATION" and not "ACCEPTED")
        {
            throw new InvalidOperationException(
                $"Cannot cancel click and collect order from {currentStatus} status.");
        }

        Status = "CANCELLED";
        FulfillmentStatus = "CANCELLED";
        CancelledAt = now;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        UpdatedAt = now;
    }

    public string GetClickAndCollectCustomerStatus()
    {
        if (string.Equals(Status, "CANCELLED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(FulfillmentStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            return "CANCELLED";

        if (string.Equals(Status, "COMPLETED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(FulfillmentStatus, "FULFILLED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(FulfillmentStatus, "COLLECTED", StringComparison.OrdinalIgnoreCase))
            return "COMPLETED";

        if (string.Equals(FulfillmentStatus, "READY", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(FulfillmentStatus, "READY_FOR_COLLECTION", StringComparison.OrdinalIgnoreCase))
            return "READY_FOR_COLLECTION";

        if (string.Equals(FulfillmentStatus, "PREPARING", StringComparison.OrdinalIgnoreCase))
            return "PREPARING";

        if (string.Equals(Status, "ACCEPTED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(FulfillmentStatus, "ACCEPTED", StringComparison.OrdinalIgnoreCase))
            return "ACCEPTED";

        return "PENDING_CONFIRMATION";
    }

    private static bool IsAllowedClickAndCollectTransition(string currentStatus, string targetStatus) =>
        (currentStatus, targetStatus) switch
        {
            ("PENDING_CONFIRMATION", "ACCEPTED") => true,
            ("ACCEPTED", "PREPARING") => true,
            ("PREPARING", "READY_FOR_COLLECTION") => true,
            ("READY_FOR_COLLECTION", "COMPLETED") => true,
            _ => false
        };


    public bool TryAssignCustomer(Guid customerId, string? customerNameSnapshot, Guid updatedByTenantUserId, DateTimeOffset now)
    {
        if (!string.Equals(Status, "DRAFT", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase) ||
            CancelledAt.HasValue)
        {
            return false;
        }

        CustomerId = customerId;
        CustomerNameSnapshot = string.IsNullOrWhiteSpace(customerNameSnapshot)
            ? null
            : customerNameSnapshot.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
        return true;
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
