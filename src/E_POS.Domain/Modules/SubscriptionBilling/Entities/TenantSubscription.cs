using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.SubscriptionBilling.Constants;

namespace E_POS.Domain.Modules.SubscriptionBilling.Entities;

public class TenantSubscription : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string SubscriptionNumber { get; protected set; } = string.Empty;
    public Guid SubscriptionPlanId { get; protected set; }
    public string SubscriptionStatus { get; protected set; } = string.Empty;

    public static TenantSubscription Create(
        Guid id,
        Guid tenantId,
        Guid subscriptionPlanId,
        string subscriptionStatus,
        DateTimeOffset createdAt)
    {
        return new TenantSubscription
        {
            Id = id,
            TenantId = tenantId,
            SubscriptionNumber = $"SUB-{id.ToString("N")[..8]}",
            SubscriptionPlanId = subscriptionPlanId,
            SubscriptionStatus = subscriptionStatus,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void Activate(DateTimeOffset now)
    {
        SubscriptionStatus = TenantSubscriptionStatusConstants.Active;
        UpdatedAt = now;
    }

    public void ChangePlan(Guid subscriptionPlanId, DateTimeOffset now)
    {
        SubscriptionPlanId = subscriptionPlanId;
        UpdatedAt = now;
    }

    public void TouchUpdatedAt(DateTimeOffset now)
    {
        UpdatedAt = now;
    }
}
