using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Entities;

public class TenantUser : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Email { get; protected set; } = string.Empty;
    public string EncryptedPassword { get; protected set; } = string.Empty;
    public string? Phone { get; protected set; }
    public string? UnmaskedPhone { get; protected set; }
    public string PasswordSalt { get; protected set; } = string.Empty;
    public string FullName { get; protected set; } = string.Empty;
    public string? DisplayName { get; protected set; }
    public Guid? ProfileImageUrl { get; protected set; }
    public Guid? OutletId { get; protected set; }
    public string DefaultOutletId { get; protected set; } = string.Empty;
    public string UserType { get; protected set; } = string.Empty;
    public string AccountStatus { get; protected set; } = string.Empty;
    public DateTimeOffset? LockedUntil { get; protected set; }
    public int FailedLoginAttempts { get; protected set; }
    public DateTimeOffset? PasswordChangeRequiredAt { get; protected set; }
    public bool AcceptedPrivacyTerms { get; protected set; }
    public string AcceptedTermsVersion { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
    public string SourceUserType { get; protected set; } = string.Empty;
    public string? Notes { get; protected set; }

    public static TenantUser Create(
        Guid id,
        Guid tenantId,
        string email,
        string fullName,
        string? phone,
        string? unmaskedPhone,
        string encryptedPassword,
        string passwordSalt,
        string accountStatus,
        string userType,
        string sourceUserType,
        string defaultOutletId,
        DateTimeOffset now)
    {
        return new TenantUser
        {
            Id = id,
            TenantId = tenantId,
            Email = NormalizeEmail(email),
            FullName = fullName.Trim(),
            Phone = phone,
            UnmaskedPhone = unmaskedPhone,
            EncryptedPassword = encryptedPassword,
            PasswordSalt = passwordSalt,
            AccountStatus = accountStatus,
            UserType = userType,
            SourceUserType = sourceUserType,
            DefaultOutletId = defaultOutletId,
            FailedLoginAttempts = 0,
            AcceptedPrivacyTerms = false,
            AcceptedTermsVersion = "1.0",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static TenantUser CreatePendingInvite(
        Guid id,
        Guid tenantId,
        string email,
        string fullName,
        string? phone,
        string? unmaskedPhone,
        DateTimeOffset now)
    {
        return Create(
            id,
            tenantId,
            email,
            fullName,
            phone,
            unmaskedPhone,
            TenantUserConstants.PendingInvitePasswordHash, // using constant as placeholder
            "empty_salt",
            TenantUserConstants.StatusInvited,
            "admin", // default
            "admin", // default
            "HQ",
            now);
    }

    public void SetPasswordHash(string encryptedPassword, string passwordSalt, DateTimeOffset now)
    {
        EncryptedPassword = encryptedPassword;
        PasswordSalt = passwordSalt;
        UpdatedAt = now;
    }

    public void UpdateAudit(Guid? updatedBy, DateTimeOffset now)
    {
        UpdatedByTenantUserId = updatedBy;
        UpdatedAt = now;
    }

    public void UpdateProfile(
        string fullName,
        string email,
        string? phone,
        string accountStatus,
        DateTimeOffset now)
    {
        FullName = fullName.Trim();
        Email = NormalizeEmail(email);
        Phone = phone;
        UnmaskedPhone = phone;
        AccountStatus = accountStatus;
        UpdatedAt = now;
    }

    public void Disable(DateTimeOffset now)
    {
        AccountStatus = TenantUserConstants.StatusInactive;
        UpdatedAt = now;
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }
}
