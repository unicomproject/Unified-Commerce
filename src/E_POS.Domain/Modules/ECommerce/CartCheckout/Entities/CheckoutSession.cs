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

    protected CheckoutSession() { }

    public static CheckoutSession CreateFromCart(
        Guid id,
        Guid tenantId,
        Guid? salesChannelId,
        Guid cartId,
        Guid customerId,
        string anonymousSessionId,
        string checkoutNumber,
        Guid selectedOutletId,
        Guid? selectedPickupSlotId,
        string? pickupContactName,
        string? pickupContactPhone,
        string? pickupContactEmail,
        string currencyCode,
        decimal subtotalAmount,
        decimal discountAmount,
        decimal taxAmount,
        decimal chargeAmount,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("A tenant is required.", nameof(tenantId));
        if (cartId == Guid.Empty) throw new ArgumentException("A cart is required.", nameof(cartId));
        if (customerId == Guid.Empty) throw new ArgumentException("A customer is required.", nameof(customerId));
        if (selectedOutletId == Guid.Empty) throw new ArgumentException("A pickup outlet is required.", nameof(selectedOutletId));
        if (string.IsNullOrWhiteSpace(anonymousSessionId)) throw new ArgumentException("A cart session is required.", nameof(anonymousSessionId));
        if (expiresAt <= now) throw new ArgumentOutOfRangeException(nameof(expiresAt));

        return new CheckoutSession
        {
            Id = id,
            TenantId = tenantId,
            SalesChannelId = salesChannelId,
            CartId = cartId,
            CustomerId = customerId,
            AnonymousSessionId = anonymousSessionId.Trim(),
            CheckoutNumber = checkoutNumber.Trim().ToUpperInvariant(),
            CheckoutStatus = "STARTED",
            SalesChannel = "ECOMMERCE_WEB",
            FulfillmentMethodCode = "CLICK_AND_COLLECT",
            SelectedOutletId = selectedOutletId,
            SelectedPickupSlotId = selectedPickupSlotId,
            PickupContactName = NormalizeOptional(pickupContactName),
            PickupContactPhone = NormalizeOptional(pickupContactPhone),
            PickupContactEmail = NormalizeOptional(pickupContactEmail),
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            SubtotalAmount = Round(subtotalAmount),
            DiscountAmount = Round(discountAmount),
            TaxAmount = Round(taxAmount),
            ChargeAmount = Round(chargeAmount),
            TotalAmount = Round(subtotalAmount - discountAmount + taxAmount + chargeAmount),
            ExpiredAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void AttachInventoryReservation(Guid reservationId, DateTimeOffset now)
    {
        if (reservationId == Guid.Empty) throw new ArgumentException("A reservation is required.", nameof(reservationId));
        InventoryReservationId = reservationId;
        CheckoutStatus = "PENDING";
        UpdatedAt = now;
    }

    public void Complete(Guid orderId, DateTimeOffset now)
    {
        if (CheckoutStatus is not ("STARTED" or "PENDING"))
            throw new InvalidOperationException("Only an active checkout session can be completed.");
        if (orderId == Guid.Empty) throw new ArgumentException("An order is required.", nameof(orderId));

        ConvertedOrderId = orderId;
        CheckoutStatus = "COMPLETED";
        CompletedAt = now;
        UpdatedAt = now;
    }

    public void Expire(DateTimeOffset now)
    {
        if (CheckoutStatus == "COMPLETED")
            throw new InvalidOperationException("A completed checkout session cannot be expired.");

        CheckoutStatus = "EXPIRED";
        ExpiredAt ??= now;
        UpdatedAt = now;
    }

    private static decimal Round(decimal value) =>
        decimal.Round(value, 4, MidpointRounding.AwayFromZero);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

