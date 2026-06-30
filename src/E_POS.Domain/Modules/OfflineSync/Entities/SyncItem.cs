using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.OfflineSync.Entities;

public class SyncItem : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OfflineClientId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string OperationType { get; protected set; } = string.Empty;
    public Guid ClientRecordId { get; protected set; }
    public string EntityName { get; protected set; } = string.Empty;
    public string PayloadHash { get; protected set; } = string.Empty;
    public Guid SyncBatchId { get; protected set; }
}
