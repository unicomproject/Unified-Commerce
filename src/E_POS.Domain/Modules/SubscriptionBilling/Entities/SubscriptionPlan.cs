using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class SubscriptionPlan : AuditableEntity
{
    public string PlanCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string BillingInterval { get; protected set; } = string.Empty;
    public decimal PriceAmount { get; protected set; }
}
