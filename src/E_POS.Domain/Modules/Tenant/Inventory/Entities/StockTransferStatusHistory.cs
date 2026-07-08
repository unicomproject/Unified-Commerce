using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class StockTransferStatusHistory : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid StockTransferId { get; protected set; }
    public int SequenceNumber { get; protected set; }
    public string? OldStatus { get; protected set; }
    public string NewStatus { get; protected set; } = string.Empty;
    public Guid? ChangedByTenantUserId { get; protected set; }
    public DateTimeOffset ChangedAt { get; protected set; }
    public string? ChangeReason { get; protected set; }

    protected StockTransferStatusHistory() { }

    public static StockTransferStatusHistory Create(
        Guid id,
        Guid tenantId,
        Guid stockTransferId,
        int sequenceNumber,
        string? oldStatus,
        string newStatus,
        Guid? changedByTenantUserId,
        string? changeReason,
        DateTimeOffset now)
    {
        return new StockTransferStatusHistory
        {
            Id = id,
            TenantId = tenantId,
            StockTransferId = stockTransferId,
            SequenceNumber = sequenceNumber,
            OldStatus = oldStatus?.Trim(),
            NewStatus = newStatus.Trim(),
            ChangedByTenantUserId = changedByTenantUserId,
            ChangedAt = now,
            ChangeReason = changeReason?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
