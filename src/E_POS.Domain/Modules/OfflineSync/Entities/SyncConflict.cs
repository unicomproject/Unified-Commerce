using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.OfflineSync.Entities;

public class SyncConflict : AuditableEntity
{
    public Guid OfflineClientId { get; protected set; }
    public string ResolutionStatus { get; protected set; } = string.Empty;
    public Guid SyncBatchId { get; protected set; }
    public Guid SyncItemId { get; protected set; }
}
