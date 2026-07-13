using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionAddonLimit : AuditableEntity
{
    public Guid SubscriptionAddonId { get; protected set; }
    public Guid FeatureLimitDefinitionId { get; protected set; }
    public decimal IncrementValue { get; protected set; }

    public static SubscriptionAddonLimit Create(
        Guid id,
        Guid subscriptionAddonId,
        Guid featureLimitDefinitionId,
        decimal incrementValue,
        DateTimeOffset now)
    {
        return new SubscriptionAddonLimit
        {
            Id = id,
            SubscriptionAddonId = subscriptionAddonId,
            FeatureLimitDefinitionId = featureLimitDefinitionId,
            IncrementValue = incrementValue,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateIncrement(decimal incrementValue, DateTimeOffset now)
    {
        IncrementValue = incrementValue;
        UpdatedAt = now;
    }
}
