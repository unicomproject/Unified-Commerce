using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionPlanAddon : AuditableEntity
{
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid SubscriptionAddonId { get; protected set; }
    public Guid SubscriptionPlanId { get; protected set; }
    public int MinQuantity { get; protected set; } = SubscriptionCatalogConstants.DefaultMinQuantity;
    public int? MaxQuantity { get; protected set; }

    public static SubscriptionPlanAddon Create(
        Guid id,
        Guid subscriptionPlanId,
        Guid subscriptionAddonId,
        string status,
        DateTimeOffset now,
        int minQuantity = SubscriptionCatalogConstants.DefaultMinQuantity,
        int? maxQuantity = null,
        string? description = null)
    {
        return new SubscriptionPlanAddon
        {
            Id = id,
            SubscriptionPlanId = subscriptionPlanId,
            SubscriptionAddonId = subscriptionAddonId,
            Status = status,
            MinQuantity = minQuantity,
            MaxQuantity = maxQuantity,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
