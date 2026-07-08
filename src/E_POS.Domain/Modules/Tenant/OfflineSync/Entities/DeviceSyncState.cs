using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.OfflineSync.Entities;

public class DeviceSyncState : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OfflineClientId { get; protected set; }
    public string DatasetName { get; protected set; } = string.Empty;
    public string SyncDirection { get; protected set; } = string.Empty;
    public string? SyncFilterJson { get; protected set; }
    public DateTimeOffset? LastFullSyncAt { get; protected set; }
    public DateTimeOffset? LastIncrementalSyncAt { get; protected set; }
    public long? LastServerVersion { get; protected set; }
    public long? LastClientVersion { get; protected set; }
    public string Status { get; protected set; } = string.Empty;

    protected DeviceSyncState() { }

    public static DeviceSyncState Create(
        Guid id,
        Guid tenantId,
        Guid offlineClientId,
        string datasetName,
        string syncDirection,
        string status,
        DateTimeOffset now)
    {
        return new DeviceSyncState
        {
            Id = id,
            TenantId = tenantId,
            OfflineClientId = offlineClientId,
            DatasetName = datasetName.Trim(),
            SyncDirection = syncDirection.Trim(),
            Status = status.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

