using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.OfflineSync.Entities;

public class SyncItem : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SyncBatchId { get; protected set; }
    public Guid OfflineClientId { get; protected set; }
    public string Direction { get; protected set; } = string.Empty;
    public string EntityName { get; protected set; } = string.Empty;
    public string? ClientRecordId { get; protected set; }
    public Guid? ServerRecordId { get; protected set; }
    public string OperationType { get; protected set; } = string.Empty;
    public string PayloadJson { get; protected set; } = string.Empty;
    public string? PayloadHash { get; protected set; }
    public string ItemStatus { get; protected set; } = string.Empty;
    public string? ErrorCode { get; protected set; }
    public string? ErrorMessage { get; protected set; }
    public DateTimeOffset? ReceivedAt { get; protected set; }
    public DateTimeOffset? ProcessedAt { get; protected set; }
}

