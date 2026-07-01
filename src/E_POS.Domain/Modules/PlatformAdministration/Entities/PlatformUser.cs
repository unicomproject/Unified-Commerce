using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PlatformAdministration.Entities;

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
            Email = email,
            NormalizedEmail = NormalizeEmail(email),
            PasswordHash = passwordHash,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };
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
