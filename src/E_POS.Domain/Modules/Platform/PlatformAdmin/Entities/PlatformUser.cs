using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

public class PlatformUser : AuditableEntity
{
    public string Email { get; protected set; } = string.Empty;
    public string NormalizedEmail { get; protected set; } = string.Empty;
    public string PasswordHash { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;

    public static PlatformUser Create(Guid id, string email, string passwordHash, string status, DateTimeOffset now)
    {
        return new PlatformUser
        {
            Id = id,
            Email = email.Trim(),
            NormalizedEmail = NormalizeEmail(email),
            PasswordHash = passwordHash,
            Status = status,
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
        UpdatedAt = now;
    }

    public void SetStatus(string status, DateTimeOffset now)
    {
        Status = status;
        UpdatedAt = now;
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }
}

