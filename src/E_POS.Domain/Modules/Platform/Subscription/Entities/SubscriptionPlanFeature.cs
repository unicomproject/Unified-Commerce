using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionPlanFeature : AuditableEntity
{
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid PlatformFeatureId { get; protected set; }
    public int SortOrder { get; protected set; }
    public Guid SubscriptionPlanId { get; protected set; }
    public string? ConfigJson { get; protected set; }
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public static SubscriptionPlanFeature CreateIncluded(
        Guid id,
        Guid subscriptionPlanId,
        Guid platformFeatureId,
        int sortOrder,
        DateTimeOffset now,
        string? description = null,
        string? configJson = null,
        Guid? createdByPlatformUserId = null)
    {
        return new SubscriptionPlanFeature
        {
            Id = id,
            SubscriptionPlanId = subscriptionPlanId,
            PlatformFeatureId = platformFeatureId,
            SortOrder = sortOrder,
            Status = SubscriptionPlanConstants.PlanFeatureStatus.Included,
            Description = description,
            ConfigJson = configJson,
            CreatedByPlatformUserId = createdByPlatformUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
