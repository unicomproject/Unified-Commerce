using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class TenantSubscription : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string SubscriptionNumber { get; protected set; } = string.Empty;
    public Guid SubscriptionPlanId { get; protected set; }
    public string SubscriptionStatus { get; protected set; } = string.Empty;
}
