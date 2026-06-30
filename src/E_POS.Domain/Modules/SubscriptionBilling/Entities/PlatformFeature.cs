using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class PlatformFeature : AuditableEntity
{
    public string FeatureCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid PlatformModuleId { get; protected set; }
    public int SortOrder { get; protected set; }
}
