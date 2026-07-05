using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Customer.Entities;

public class CustomerAuthAccount : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid CustomerId { get; protected set; }
    public int FailedLoginCount { get; protected set; }
    public string? PasswordHash { get; protected set; }
    public DateTimeOffset? EmailVerifiedAt { get; protected set; }
    public DateTimeOffset? PhoneVerifiedAt { get; protected set; }
    public DateTimeOffset? LastFailedLoginAt { get; protected set; }
    public DateTimeOffset? LockedUntil { get; protected set; }
    public DateTimeOffset? LastLoginAt { get; protected set; }
    public DateTimeOffset? LastPasswordChangedAt { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}
