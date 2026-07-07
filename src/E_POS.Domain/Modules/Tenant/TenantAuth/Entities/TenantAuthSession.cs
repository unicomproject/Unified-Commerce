using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

public class TenantAuthSession : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid UserId { get; protected set; }
    public string? IpAddress { get; protected set; }
    public string? UserAgent { get; protected set; }
    public string? DeviceName { get; protected set; }
    public Guid? PosDeviceId { get; protected set; }
    public DateTimeOffset? LastSeenAt { get; protected set; }
    public DateTimeOffset ExpiresAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public Guid? RevokedByTenantUserId { get; protected set; }
    public Guid? RevokedByPlatformUserId { get; protected set; }
    public string? RevokeReason { get; protected set; }

    public static TenantAuthSession Create(
        Guid id, 
        Guid tenantId, 
        Guid userId, 
        string? ipAddress, 
        string? userAgent, 
        DateTimeOffset expiresAt, 
        DateTimeOffset now)
    {
        return new TenantAuthSession
        {
            Id = id,
            TenantId = tenantId,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Revoke(DateTimeOffset now, Guid? revokedByTenantUserId = null, Guid? revokedByPlatformUserId = null, string? reason = null)
    {
        if (RevokedAt.HasValue) return;
        RevokedAt = now;
        RevokedByTenantUserId = revokedByTenantUserId;
        RevokedByPlatformUserId = revokedByPlatformUserId;
        RevokeReason = reason;
        UpdatedAt = now;
    }
}
