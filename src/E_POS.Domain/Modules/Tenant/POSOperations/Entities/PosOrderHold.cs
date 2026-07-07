using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.POSOperations.Entities;

public class PosOrderHold : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string HoldNumber { get; protected set; } = string.Empty;
    public Guid SalesOrderId { get; protected set; }
    public string HoldStatus { get; protected set; } = string.Empty;
    public string? HoldReason { get; protected set; }
    public Guid HeldByTenantUserId { get; protected set; }
    public DateTimeOffset HeldAt { get; protected set; }
    public Guid? ReleasedByTenantUserId { get; protected set; }
    public DateTimeOffset? ReleasedAt { get; protected set; }
    public DateTimeOffset? ExpiresAt { get; protected set; }
    public DateTimeOffset? CancelledAt { get; protected set; }
    public string? CancellationReason { get; protected set; }
}

