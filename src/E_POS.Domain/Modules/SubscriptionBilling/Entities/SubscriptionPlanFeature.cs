using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.SubscriptionBilling.Constants;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class SubscriptionPlanFeature : AuditableEntity
{
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid PlatformFeatureId { get; protected set; }
    public int SortOrder { get; protected set; }
    public Guid SubscriptionPlanId { get; protected set; }

    public static SubscriptionPlanFeature CreateIncluded(
        Guid id,
        Guid subscriptionPlanId,
        Guid platformFeatureId,
        int sortOrder,
        DateTimeOffset now,
        string? description = null)
    {
        return new SubscriptionPlanFeature
        {
            Id = id,
            SubscriptionPlanId = subscriptionPlanId,
            PlatformFeatureId = platformFeatureId,
            SortOrder = sortOrder,
            Status = SubscriptionPlanConstants.PlanFeatureStatus.Included,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
