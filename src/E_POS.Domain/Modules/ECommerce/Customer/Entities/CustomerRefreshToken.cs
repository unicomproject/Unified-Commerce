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
}
