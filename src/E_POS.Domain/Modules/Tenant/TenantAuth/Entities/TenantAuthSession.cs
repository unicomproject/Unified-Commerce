using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;

namespace E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

public class TenantAuthSession : AuditableEntity
{
    public Guid? TenantUserId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string SessionTokenHash { get; protected set; } = string.Empty;

    public static TenantAuthSession Create(Guid id, Guid tenantUserId, string sessionTokenHash, DateTimeOffset now)
    {
        return new TenantAuthSession
        {
            Id = id,
            TenantUserId = tenantUserId,
            Status = TenantAuthConstants.ActiveTokenStatus,
            SessionTokenHash = sessionTokenHash,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Revoke(DateTimeOffset now)
    {
        if (Status == TenantAuthConstants.RevokedTokenStatus)
        {
            return;
        }

        Status = TenantAuthConstants.RevokedTokenStatus;
        UpdatedAt = now;
    }
}
