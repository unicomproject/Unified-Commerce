using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class TenantFeatureEntitlement : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string EntitlementStatus { get; protected set; } = string.Empty;
    public Guid PlatformFeatureId { get; protected set; }

    public static TenantFeatureEntitlement Create(
        Guid id,
        Guid tenantId,
        Guid platformFeatureId,
        string entitlementStatus,
        DateTimeOffset createdAt)
    {
        return new TenantFeatureEntitlement
        {
            Id = id,
            TenantId = tenantId,
            PlatformFeatureId = platformFeatureId,
            EntitlementStatus = entitlementStatus,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }
}
