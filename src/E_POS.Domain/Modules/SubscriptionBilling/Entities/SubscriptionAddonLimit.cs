using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class SubscriptionAddonLimit : AuditableEntity
{
    public Guid FeatureLimitDefinitionId { get; protected set; }
    public int? LimitValue { get; protected set; }
    public Guid SubscriptionAddonFeatureId { get; protected set; }
}
