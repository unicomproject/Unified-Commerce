using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class SubscriptionPlanFeature : AuditableEntity
{
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid PlatformFeatureId { get; protected set; }
    public int SortOrder { get; protected set; }
    public Guid SubscriptionPlanId { get; protected set; }
}
