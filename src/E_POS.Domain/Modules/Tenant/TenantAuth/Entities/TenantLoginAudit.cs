using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

public class TenantLoginAudit : AuditableEntity
{
    public Guid? TenantUserId { get; protected set; }
    public string LoginResult { get; protected set; } = string.Empty;

    public static TenantLoginAudit Create(Guid id, Guid? tenantUserId, string loginResult, DateTimeOffset now)
    {
        return new TenantLoginAudit
        {
            Id = id,
            TenantUserId = tenantUserId,
            LoginResult = loginResult,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
