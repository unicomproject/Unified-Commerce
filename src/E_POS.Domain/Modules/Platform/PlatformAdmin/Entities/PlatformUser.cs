using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformUser : AuditableEntity
{
    public string Email { get; protected set; } = string.Empty;
    public string NormalizedEmail { get; protected set; } = string.Empty;
    public string PasswordHash { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string? FirstName { get; protected set; }
    public string? LastName { get; protected set; }
    public string? DisplayName { get; protected set; }
    public string? Phone { get; protected set; }
    public string? JobTitle { get; protected set; }
    public DateTimeOffset? EmailVerifiedAt { get; protected set; }
    public int FailedLoginCount { get; protected set; }
    public DateTimeOffset? LockedUntil { get; protected set; }
    public DateTimeOffset? LastLoginAt { get; protected set; }
    public DateTimeOffset? PasswordChangedAt { get; protected set; }
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public static PlatformUser Create(Guid id, string email, string passwordHash, string status, DateTimeOffset now)
    {
        return new PlatformUser
        {
            Id = id,
            Email = email.Trim(),
            NormalizedEmail = NormalizeEmail(email),
            PasswordHash = passwordHash,
            Status = status,
            FailedLoginCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static PlatformUser CreatePendingInvite(Guid id, string email, DateTimeOffset now)
    {
        return Create(
            id,
            email,
            PlatformUserConstants.PendingInvitePasswordHash,
            PlatformAuthConstants.InactiveStatus,
            now);
    }

    public void TouchUpdatedAt(DateTimeOffset now)
    {
        UpdatedAt = now;
    }

    public void SetPasswordHash(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        PasswordChangedAt = now;
        UpdatedAt = now;
    }

    public void SetStatus(string status, DateTimeOffset now)
    {
        Status = status;
        UpdatedAt = now;
    }

    public void RecordSuccessfulLogin(DateTimeOffset now)
    {
        LastLoginAt = now;
        FailedLoginCount = 0;
        LockedUntil = null;
        UpdatedAt = now;
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }
}
