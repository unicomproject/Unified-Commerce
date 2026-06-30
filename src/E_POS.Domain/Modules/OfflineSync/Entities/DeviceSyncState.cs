using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.OfflineSync.Entities;

public class DeviceSyncState : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OfflineClientId { get; protected set; }
    public string DatasetName { get; protected set; } = string.Empty;
    public int LastClientVersion { get; protected set; }
    public int LastServerVersion { get; protected set; }
}
