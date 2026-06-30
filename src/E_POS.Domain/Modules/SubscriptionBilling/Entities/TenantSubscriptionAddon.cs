using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class TenantSubscriptionAddon : AuditableEntity
{
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid SubscriptionAddonId { get; protected set; }
    public Guid TenantSubscriptionId { get; protected set; }
}
