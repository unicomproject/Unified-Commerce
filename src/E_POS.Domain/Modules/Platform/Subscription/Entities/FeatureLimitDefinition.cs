using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class FeatureLimitDefinition : AuditableEntity
{
    public string LimitCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public int? DefaultLimitValue { get; protected set; }
    public Guid PlatformFeatureId { get; protected set; }
}

