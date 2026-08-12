using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

public sealed class TenantUserInviteDeliverySecret : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid TenantUserId { get; private set; }
    public Guid InviteId { get; private set; }
    public string EncryptedToken { get; private set; } = string.Empty;
    public string KeyVersion { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? PurgedAt { get; private set; }

    public static TenantUserInviteDeliverySecret Create(Guid id, Guid tenantId, Guid tenantUserId,
        Guid inviteId, string encryptedToken, string keyVersion, DateTimeOffset expiresAt, DateTimeOffset now) => new()
    {
        Id = id, TenantId = tenantId, TenantUserId = tenantUserId, InviteId = inviteId,
        EncryptedToken = encryptedToken, KeyVersion = keyVersion, ExpiresAt = expiresAt,
        CreatedAt = now, UpdatedAt = now
    };

    public void Purge(DateTimeOffset now)
    {
        EncryptedToken = string.Empty;
        PurgedAt = now;
        UpdatedAt = now;
    }
}
