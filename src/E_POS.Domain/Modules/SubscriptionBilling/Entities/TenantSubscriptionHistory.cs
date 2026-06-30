using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class TenantSubscriptionHistory : AuditableEntity
{
    public int SequenceNumber { get; protected set; }
    public Guid TenantSubscriptionId { get; protected set; }
}
