using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.AuthSecurity.Entities;

public class TenantRefreshToken : AuditableEntity
{
    public string Status { get; protected set; } = string.Empty;
    public Guid TenantAuthSessionId { get; protected set; }
    public string TokenHash { get; protected set; } = string.Empty;

    public static TenantRefreshToken Create(Guid id, Guid tenantAuthSessionId, string tokenHash, DateTimeOffset now)
    {
        return new TenantRefreshToken
        {
            Id = id,
            TenantAuthSessionId = tenantAuthSessionId,
            Status = "ACTIVE",
            TokenHash = tokenHash,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}