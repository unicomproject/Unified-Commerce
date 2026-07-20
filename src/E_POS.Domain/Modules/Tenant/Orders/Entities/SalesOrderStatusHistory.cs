using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Orders.Entities;

public class SalesOrderStatusHistory : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesOrderId { get; protected set; }
    public int SequenceNumber { get; protected set; }
    public string StatusType { get; protected set; } = string.Empty;
    public string? OldStatus { get; protected set; }
    public string NewStatus { get; protected set; } = string.Empty;
    public Guid? ChangedByTenantUserId { get; protected set; }
    public DateTimeOffset ChangedAt { get; protected set; }
    public string? ChangeReason { get; protected set; }

    public static SalesOrderStatusHistory Create(
        Guid id,
        Guid tenantId,
        Guid salesOrderId,
        int sequenceNumber,
        string statusType,
        string? oldStatus,
        string newStatus,
        Guid? changedByTenantUserId,
        DateTimeOffset changedAt,
        string? changeReason = null)
    {
        return new SalesOrderStatusHistory
        {
            Id = id,
            TenantId = tenantId,
            SalesOrderId = salesOrderId,
            SequenceNumber = sequenceNumber,
            StatusType = statusType.Trim().ToUpperInvariant(),
            OldStatus = string.IsNullOrWhiteSpace(oldStatus) ? null : oldStatus.Trim().ToUpperInvariant(),
            NewStatus = newStatus.Trim().ToUpperInvariant(),
            ChangedByTenantUserId = changedByTenantUserId,
            ChangedAt = changedAt,
            ChangeReason = string.IsNullOrWhiteSpace(changeReason) ? null : changeReason.Trim()
        };
    }
}

