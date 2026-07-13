using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionAddonFeature : AuditableEntity
{
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid PlatformFeatureId { get; protected set; }
    public int SortOrder { get; protected set; }
    public Guid SubscriptionAddonId { get; protected set; }
    public string? ConfigJson { get; protected set; }

    public static SubscriptionAddonFeature Create(
        Guid id,
        Guid subscriptionAddonId,
        Guid platformFeatureId,
        string status,
        DateTimeOffset now,
        int sortOrder = 0,
        string? description = null,
        string? configJson = null)
    {
        return new SubscriptionAddonFeature
        {
            Id = id,
            SubscriptionAddonId = subscriptionAddonId,
            PlatformFeatureId = platformFeatureId,
            Status = status,
            SortOrder = sortOrder,
            Description = description,
            ConfigJson = configJson,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
