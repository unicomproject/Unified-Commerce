using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.AccessControl.Constants;

namespace E_POS.Domain.Modules.AccessControl.Entities;

public class TenantUser : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string? FirstName { get; protected set; }
    public string? LastName { get; protected set; }
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
        return Create(
            id,
            tenantId,
            email,
            firstName: null,
            lastName: null,
            normalizedPhone,
            passwordHash,
            status,
            now);
    }

    public static TenantUser Create(
        Guid id,
        Guid tenantId,
        string email,
        string? firstName,
        string? lastName,
        string? normalizedPhone,
        string? passwordHash,
        string status,
        DateTimeOffset now)
    {
        return new TenantUser
        {
            Id = id,
            TenantId = tenantId,
            FirstName = NormalizeOptionalText(firstName),
            LastName = NormalizeOptionalText(lastName),
            NormalizedEmail = NormalizeEmail(email),
            NormalizedPhone = normalizedPhone ?? string.Empty,
            PasswordHash = passwordHash,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static TenantUser CreatePendingInvite(
        Guid id,
        Guid tenantId,
        string email,
        string? firstName,
        string? lastName,
        string? normalizedPhone,
        DateTimeOffset now)
    {
        return Create(
            id,
            tenantId,
            email,
            firstName,
            lastName,
            normalizedPhone,
            TenantUserConstants.PendingInvitePasswordHash,
            TenantUserConstants.StatusInvited,
            now);
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

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
