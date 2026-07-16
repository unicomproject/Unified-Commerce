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

    protected ShoppingCart() { }

    public static ShoppingCart Create(
        Guid tenantId,
        Guid? salesChannelId,
        Guid? customerId,
        string? anonymousSessionId,
        string cartNumber,
        string currencyCode,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        if (!customerId.HasValue && string.IsNullOrWhiteSpace(anonymousSessionId))
            throw new ArgumentException("A customer or anonymous session is required.");

        return new ShoppingCart
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SalesChannelId = salesChannelId,
            CustomerId = customerId,
            AnonymousSessionId = anonymousSessionId?.Trim(),
            CartNumber = cartNumber.Trim().ToUpperInvariant(),
            SalesChannel = "ECOMMERCE_WEB",
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            CartStatus = "ACTIVE",
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateTotals(decimal subtotal, decimal discount, decimal tax, decimal charge, DateTimeOffset now)
    {
        SubtotalAmount = decimal.Round(subtotal, 4, MidpointRounding.AwayFromZero);
        DiscountAmount = decimal.Round(discount, 4, MidpointRounding.AwayFromZero);
        TaxAmount = decimal.Round(tax, 4, MidpointRounding.AwayFromZero);
        ChargeAmount = decimal.Round(charge, 4, MidpointRounding.AwayFromZero);
        TotalAmount = decimal.Round(SubtotalAmount - DiscountAmount + TaxAmount + ChargeAmount, 4, MidpointRounding.AwayFromZero);
        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        CartStatus = "CANCELLED";
        UpdatedAt = now;
    }
}

