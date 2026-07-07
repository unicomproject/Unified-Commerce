using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.OfflineSync.Entities;

public class OfflineClient : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid PosDeviceId { get; protected set; }
    public string ClientCode { get; protected set; } = string.Empty;
    public string ClientName { get; protected set; } = string.Empty;
    public string OfflineType { get; protected set; } = string.Empty;
    public bool OfflineEnabled { get; protected set; }
    public int? MaxOfflineDurationMinutes { get; protected set; }
    public string? ClientKeyHash { get; protected set; }
    public DateTimeOffset? LastSeenAt { get; protected set; }
    public DateTimeOffset? LastSyncAt { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
}

