using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class TenantSubscriptionHistory : AuditableEntity
{
    public int SequenceNumber { get; protected set; }
    public Guid TenantId { get; protected set; }
    public Guid TenantSubscriptionId { get; protected set; }
    public Guid SubscriptionId { get; protected set; }
    public Guid? OldPlanId { get; protected set; }
    public Guid? NewPlanId { get; protected set; }
    public string? OldStatus { get; protected set; }
    public string? NewStatus { get; protected set; }
    public string ChangeType { get; protected set; } = string.Empty;
    public string? Reason { get; protected set; }
    public string? ChangeData { get; protected set; }
    public DateTimeOffset ChangedAt { get; protected set; }
    public Guid? ChangedByPlatformUserId { get; protected set; }

    public static TenantSubscriptionHistory CreateEvent(
        Guid id,
        Guid tenantId,
        Guid tenantSubscriptionId,
        int sequenceNumber,
        string changeType,
        DateTimeOffset changedAt,
        Guid? oldPlanId = null,
        Guid? newPlanId = null,
        string? oldStatus = null,
        string? newStatus = null,
        string? reason = null,
        string? changeData = null,
        Guid? changedByPlatformUserId = null)
    {
        return new TenantSubscriptionHistory
        {
            Id = id,
            TenantId = tenantId,
            TenantSubscriptionId = tenantSubscriptionId,
            SubscriptionId = tenantSubscriptionId,
            SequenceNumber = sequenceNumber,
            ChangeType = changeType,
            ChangedAt = changedAt,
            OldPlanId = oldPlanId,
            NewPlanId = newPlanId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Reason = reason,
            ChangeData = changeData,
            ChangedByPlatformUserId = changedByPlatformUserId,
            CreatedAt = changedAt,
            UpdatedAt = changedAt
        };
    }
}

