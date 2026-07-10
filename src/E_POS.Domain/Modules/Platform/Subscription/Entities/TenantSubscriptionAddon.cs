using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class TenantSubscriptionAddon : AuditableEntity
{
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid SubscriptionAddonId { get; protected set; }
    public Guid TenantSubscriptionId { get; protected set; }
    public Guid AddonId { get; protected set; }
    public Guid SubscriptionId { get; protected set; }
    public int Quantity { get; protected set; } = 1;
    public decimal UnitPrice { get; protected set; }
    public string CurrencyCode { get; protected set; } = "LKR";
    public bool AutoRenew { get; protected set; } = true;
    public DateTimeOffset StartsAt { get; protected set; }
    public DateTimeOffset? EndsAt { get; protected set; }
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public static TenantSubscriptionAddon Create(
        Guid id,
        Guid tenantSubscriptionId,
        Guid subscriptionAddonId,
        int quantity,
        string status,
        decimal unitPrice,
        string currencyCode,
        bool autoRenew,
        DateTimeOffset? startsAt,
        DateTimeOffset? endsAt,
        Guid? createdByPlatformUserId,
        Guid? updatedByPlatformUserId,
        DateTimeOffset now)
    {
        return new TenantSubscriptionAddon
        {
            Id = id,
            TenantSubscriptionId = tenantSubscriptionId,
            SubscriptionAddonId = subscriptionAddonId,
            SubscriptionId = tenantSubscriptionId,
            AddonId = subscriptionAddonId,
            Quantity = quantity,
            Status = status,
            UnitPrice = unitPrice,
            CurrencyCode = NormalizeCurrencyCode(currencyCode),
            AutoRenew = autoRenew,
            StartsAt = startsAt ?? now,
            EndsAt = endsAt,
            CreatedByPlatformUserId = createdByPlatformUserId,
            UpdatedByPlatformUserId = updatedByPlatformUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static string NormalizeCurrencyCode(string? currencyCode)
    {
        var normalized = currencyCode?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? "LKR"
            : normalized.ToUpperInvariant();
    }
}

