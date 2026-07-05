using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Orders.Entities;

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
}
