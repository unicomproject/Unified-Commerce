using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.AuthSecurity.Constants;

namespace E_POS.Domain.Modules.AuthSecurity.Entities;

public class TenantRefreshToken : AuditableEntity
{
    public string Status { get; protected set; } = string.Empty;
    public Guid TenantAuthSessionId { get; protected set; }
    public string TokenHash { get; protected set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; protected set; }

    public static TenantRefreshToken Create(
        Guid id,
        Guid tenantAuthSessionId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        return new TenantRefreshToken
        {
            Id = id,
            TenantAuthSessionId = tenantAuthSessionId,
            Status = TenantAuthConstants.ActiveTokenStatus,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
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