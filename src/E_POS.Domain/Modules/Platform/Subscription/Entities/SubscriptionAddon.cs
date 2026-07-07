using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class SubscriptionAddon : AuditableEntity
{
    public string AddonCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public decimal PriceAmount { get; protected set; }
}

