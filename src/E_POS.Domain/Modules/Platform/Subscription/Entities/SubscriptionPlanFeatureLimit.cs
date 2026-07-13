using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionPlanFeatureLimit : AuditableEntity
{
    public Guid SubscriptionPlanId { get; protected set; }
    public Guid FeatureLimitDefinitionId { get; protected set; }
    public decimal? LimitValue { get; protected set; }
    public bool IsUnlimited { get; protected set; }

    public static SubscriptionPlanFeatureLimit Create(
        Guid id,
        Guid subscriptionPlanId,
        Guid featureLimitDefinitionId,
        decimal? limitValue,
        bool isUnlimited,
        DateTimeOffset now)
    {
        return new SubscriptionPlanFeatureLimit
        {
            Id = id,
            SubscriptionPlanId = subscriptionPlanId,
            FeatureLimitDefinitionId = featureLimitDefinitionId,
            LimitValue = limitValue,
            IsUnlimited = isUnlimited,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateLimit(decimal? limitValue, bool isUnlimited, DateTimeOffset now)
    {
        LimitValue = limitValue;
        IsUnlimited = isUnlimited;
        UpdatedAt = now;
    }
}
