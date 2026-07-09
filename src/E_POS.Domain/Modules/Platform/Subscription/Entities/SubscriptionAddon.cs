using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionAddon : AuditableEntity
{
    public string AddonCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public decimal PriceAmount { get; protected set; }
    public string AddonName { get; protected set; } = string.Empty;
    public string AddonType { get; protected set; } = SubscriptionCatalogConstants.AddonType.Capacity;
    public string BillingCycle { get; protected set; } = SubscriptionCatalogConstants.DefaultBillingCycle;
    public string BaseCurrencyCode { get; protected set; } = SubscriptionCatalogConstants.DefaultBaseCurrency;
    public decimal BasePrice { get; protected set; }
    public bool QuantityBased { get; protected set; }
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public static SubscriptionAddon Create(
        Guid id,
        string addonCode,
        string name,
        string status,
        decimal priceAmount,
        DateTimeOffset now,
        string? description = null,
        string addonType = SubscriptionCatalogConstants.AddonType.Capacity,
        string billingCycle = SubscriptionCatalogConstants.DefaultBillingCycle,
        string baseCurrencyCode = SubscriptionCatalogConstants.DefaultBaseCurrency,
        bool quantityBased = false,
        Guid? createdByPlatformUserId = null)
    {
        return new SubscriptionAddon
        {
            Id = id,
            AddonCode = addonCode,
            Name = name,
            Description = description,
            Status = status,
            PriceAmount = priceAmount,
            AddonName = name,
            AddonType = addonType,
            BillingCycle = billingCycle,
            BaseCurrencyCode = baseCurrencyCode,
            BasePrice = priceAmount,
            QuantityBased = quantityBased,
            CreatedByPlatformUserId = createdByPlatformUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
