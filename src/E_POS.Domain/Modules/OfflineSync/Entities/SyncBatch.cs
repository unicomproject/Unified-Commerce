using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.OfflineSync.Entities;

public class SyncBatch : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OfflineClientId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public int ConflictCount { get; protected set; }
    public int DownloadedItemCount { get; protected set; }
    public string IdempotencyKey { get; protected set; } = string.Empty;
    public int UploadedItemCount { get; protected set; }
}
