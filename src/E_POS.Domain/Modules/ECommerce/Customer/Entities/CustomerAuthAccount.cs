using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.Customer.Entities;

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

    protected CustomerAuthAccount() { }

    public static CustomerAuthAccount Create(
        Guid id,
        Guid tenantId,
        Guid customerId,
        string passwordHash,
        DateTimeOffset now)
    {
        return new CustomerAuthAccount
        {
            Id = id,
            TenantId = tenantId,
            CustomerId = customerId,
            PasswordHash = passwordHash,
            Status = "ACTIVE",
            LastPasswordChangedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public bool IsLocked(DateTimeOffset now) =>
        string.Equals(Status, "LOCKED", StringComparison.OrdinalIgnoreCase) &&
        (!LockedUntil.HasValue || LockedUntil > now);

    public void RecordFailedLogin(DateTimeOffset now, int maxAttempts, TimeSpan lockDuration)
    {
        if (string.Equals(Status, "LOCKED", StringComparison.OrdinalIgnoreCase) &&
            LockedUntil.HasValue && LockedUntil <= now)
        {
            Status = "ACTIVE";
            FailedLoginCount = 0;
            LockedUntil = null;
        }

        FailedLoginCount++;
        LastFailedLoginAt = now;
        if (FailedLoginCount >= maxAttempts)
        {
            Status = "LOCKED";
            LockedUntil = now.Add(lockDuration);
        }
        UpdatedAt = now;
    }

    public void RecordSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginCount = 0;
        LastFailedLoginAt = null;
        LockedUntil = null;
        LastLoginAt = now;
        if (string.Equals(Status, "LOCKED", StringComparison.OrdinalIgnoreCase))
            Status = "ACTIVE";
        UpdatedAt = now;
    }
}
