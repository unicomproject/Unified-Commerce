using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.OfflineSync.Entities;

public class SyncConflict : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OfflineClientId { get; protected set; }
    public Guid SyncBatchId { get; protected set; }
    public Guid? SyncItemId { get; protected set; }
    public string EntityName { get; protected set; } = string.Empty;
    public string? ClientRecordId { get; protected set; }
    public Guid? ServerRecordId { get; protected set; }
    public string ConflictType { get; protected set; } = string.Empty;
    public string? ClientPayloadJson { get; protected set; }
    public string? ServerPayloadJson { get; protected set; }
    public string ResolutionStatus { get; protected set; } = string.Empty;
    public string? ResolutionStrategy { get; protected set; }
    public string? ResolutionNote { get; protected set; }
    public Guid? ResolvedByTenantUserId { get; protected set; }
    public DateTimeOffset? ResolvedAt { get; protected set; }
}

