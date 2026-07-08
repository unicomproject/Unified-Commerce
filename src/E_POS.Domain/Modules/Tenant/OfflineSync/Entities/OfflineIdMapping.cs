using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.OfflineSync.Entities;

public class OfflineIdMapping : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OfflineClientId { get; protected set; }
    public string EntityName { get; protected set; } = string.Empty;
    public string ClientRecordId { get; protected set; } = string.Empty;
    public Guid ServerRecordId { get; protected set; }
    public Guid? CreatedFromSyncItemId { get; protected set; }

    protected OfflineIdMapping() { }

    public static OfflineIdMapping Create(
        Guid id,
        Guid tenantId,
        Guid offlineClientId,
        string entityName,
        string clientRecordId,
        Guid serverRecordId,
        Guid? createdFromSyncItemId,
        DateTimeOffset now)
    {
        return new OfflineIdMapping
        {
            Id = id,
            TenantId = tenantId,
            OfflineClientId = offlineClientId,
            EntityName = entityName.Trim(),
            ClientRecordId = clientRecordId.Trim(),
            ServerRecordId = serverRecordId,
            CreatedFromSyncItemId = createdFromSyncItemId,
            CreatedAt = now
        };
    }
}

