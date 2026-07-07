using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionAddonFeature : AuditableEntity
{
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid PlatformFeatureId { get; protected set; }
    public int SortOrder { get; protected set; }
    public Guid SubscriptionAddonId { get; protected set; }
}

