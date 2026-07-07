using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionPlanAddon : AuditableEntity
{
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid SubscriptionAddonId { get; protected set; }
    public Guid SubscriptionPlanId { get; protected set; }
}

