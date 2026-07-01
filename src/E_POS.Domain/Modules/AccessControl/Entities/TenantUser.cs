using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.AccessControl.Entities;

public class TenantUser : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string NormalizedEmail { get; protected set; } = string.Empty;
    public string? NormalizedPhone { get; protected set; }
    public string? PasswordHash { get; protected set; }
    public string Status { get; protected set; } = string.Empty;

    public static TenantUser Create(
        Guid id,
        Guid tenantId,
        string email,
        string? normalizedPhone,
        string? passwordHash,
        string status,
        DateTimeOffset now)
    {
        return new TenantUser
        {
            Id = id,
            TenantId = tenantId,
            NormalizedEmail = NormalizeEmail(email),
            NormalizedPhone = normalizedPhone ?? string.Empty,
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

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }
}