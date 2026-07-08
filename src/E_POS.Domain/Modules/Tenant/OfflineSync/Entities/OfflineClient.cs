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

    protected OfflineClient() { }

    public static OfflineClient Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        Guid posDeviceId,
        string clientCode,
        string clientName,
        string offlineType,
        bool offlineEnabled,
        int? maxOfflineDurationMinutes,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new OfflineClient
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            PosDeviceId = posDeviceId,
            ClientCode = clientCode.Trim(),
            ClientName = clientName.Trim(),
            OfflineType = offlineType.Trim(),
            OfflineEnabled = offlineEnabled,
            MaxOfflineDurationMinutes = maxOfflineDurationMinutes,
            Status = status.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByTenantUserId = createdByTenantUserId
        };
    }
}

