using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class TenantUsageCounter : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid PlatformFeatureId { get; protected set; }
    public string UsagePeriodStart { get; protected set; } = string.Empty;
    public decimal UsedQuantity { get; protected set; }
}

