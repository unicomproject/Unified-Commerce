using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class FeatureFlag : AuditableEntity
{
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string FlagCode { get; protected set; } = string.Empty;
    public Guid PlatformFeatureId { get; protected set; }
}
