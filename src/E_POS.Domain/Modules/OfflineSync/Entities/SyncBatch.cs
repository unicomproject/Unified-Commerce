using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.OfflineSync.Entities;

public class SyncBatch : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OfflineClientId { get; protected set; }
    public string SyncType { get; protected set; } = string.Empty;
    public string SyncStatus { get; protected set; } = string.Empty;
    public DateTimeOffset? ClientStartedAt { get; protected set; }
    public DateTimeOffset ServerStartedAt { get; protected set; }
    public DateTimeOffset? CompletedAt { get; protected set; }
    public DateTimeOffset? FailedAt { get; protected set; }
    public string? FailureReason { get; protected set; }
    public int UploadedItemCount { get; protected set; }
    public int DownloadedItemCount { get; protected set; }
    public int ConflictCount { get; protected set; }
    public string? ClientAppVersion { get; protected set; }
    public DateTimeOffset? ClientLocalTime { get; protected set; }
    public string? IdempotencyKey { get; protected set; }
}
