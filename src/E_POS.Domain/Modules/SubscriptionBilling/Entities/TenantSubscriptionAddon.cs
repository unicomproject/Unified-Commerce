using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class TenantSubscriptionAddon : AuditableEntity
{
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid SubscriptionAddonId { get; protected set; }
    public Guid TenantSubscriptionId { get; protected set; }
    public int Quantity { get; protected set; } = 1;

    public static TenantSubscriptionAddon Create(
        Guid id,
        Guid tenantSubscriptionId,
        Guid subscriptionAddonId,
        int quantity,
        string status,
        DateTimeOffset now)
    {
        return new TenantSubscriptionAddon
        {
            Id = id,
            TenantSubscriptionId = tenantSubscriptionId,
            SubscriptionAddonId = subscriptionAddonId,
            Quantity = quantity,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
