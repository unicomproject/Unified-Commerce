using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.Customer.Entities;

public class CustomerRefreshToken : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid CustomerAuthSessionId { get; protected set; }
    public string TokenHash { get; protected set; } = string.Empty;
    public Guid TokenFamilyId { get; protected set; }
    public Guid? ReplacedByTokenId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; protected set; }
    public DateTimeOffset? UsedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public string? RevokedReason { get; protected set; }

    protected CustomerRefreshToken() { }

    public static CustomerRefreshToken Create(
        Guid id,
        Guid tenantId,
        Guid customerAuthSessionId,
        string tokenHash,
        Guid tokenFamilyId,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        return new CustomerRefreshToken
        {
            Id = id,
            TenantId = tenantId,
            CustomerAuthSessionId = customerAuthSessionId,
            TokenHash = tokenHash,
            TokenFamilyId = tokenFamilyId,
            Status = "ACTIVE",
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkRotated(Guid replacementTokenId, DateTimeOffset now)
    {
        if (UsedAt.HasValue ||
            RevokedAt.HasValue ||
            ReplacedByTokenId.HasValue ||
            !string.Equals(Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Refresh token has already been consumed.");
        }

        Status = "USED";
        UsedAt = now;
        ReplacedByTokenId = replacementTokenId;
        UpdatedAt = now;
    }

    public void Revoke(DateTimeOffset now, string reason)
    {
        if (RevokedAt.HasValue) return;

        Status = "REVOKED";
        RevokedAt = now;
        RevokedReason = reason.Trim();
        UpdatedAt = now;
    }
}
