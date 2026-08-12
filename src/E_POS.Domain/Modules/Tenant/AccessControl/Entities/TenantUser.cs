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
    public string? DefaultOutletId { get; protected set; }
    public string? EmployeeId { get; protected set; }
    public string? StaffCode { get; protected set; }
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
        string? defaultOutletId,
        DateTimeOffset now,
        string? employeeId = null,
        string? staffCode = null)
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
            DefaultOutletId = string.IsNullOrWhiteSpace(defaultOutletId) ? null : defaultOutletId.Trim(),
            EmployeeId = NormalizeOptional(employeeId),
            StaffCode = NormalizeOptional(staffCode),
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
        DateTimeOffset now,
        string? staffCode = null)
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
            null,
            now,
            staffCode: staffCode);
    }

    public void SetPasswordHash(string encryptedPassword, string passwordSalt, DateTimeOffset now)
    {
        EncryptedPassword = encryptedPassword;
        PasswordSalt = passwordSalt;
        UpdatedAt = now;
    }

    /// <summary>
    /// Completes first-time invitation activation: sets a real password hash and ACTIVE status together.
    /// </summary>
    public void ActivateFromInvitation(string encryptedPassword, string passwordSalt, DateTimeOffset now)
    {
        if (!string.Equals(AccountStatus, TenantUserConstants.StatusInvited, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only INVITED users can be activated from an invitation.");
        }

        EncryptedPassword = encryptedPassword;
        PasswordSalt = passwordSalt;
        AccountStatus = TenantUserConstants.StatusActive;
        FailedLoginAttempts = 0;
        LockedUntil = null;
        UpdatedAt = now;
    }

    public void UpdateAudit(Guid? updatedBy, DateTimeOffset now)
    {
        UpdatedByTenantUserId = updatedBy;
        UpdatedAt = now;
    }

    public void SetProfileMediaAsset(Guid? mediaAssetId, Guid? updatedBy, DateTimeOffset now)
    {
        // Legacy column name profile_image_url stores the tenant user's MediaAsset identifier.
        ProfileImageUrl = mediaAssetId;
        UpdatedByTenantUserId = updatedBy;
        UpdatedAt = now;
    }

    public void AssignStaffCode(string staffCode, DateTimeOffset now)
    {
        var normalized = NormalizeOptional(staffCode);
        if (normalized is null)
        {
            throw new InvalidOperationException("Staff code is required.");
        }

        if (!string.IsNullOrWhiteSpace(StaffCode) &&
            !string.Equals(StaffCode, normalized, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Staff code is immutable.");
        }

        StaffCode = normalized;
        UpdatedAt = now;
    }

    public void UpdateProfile(
        string fullName,
        string email,
        string? phone,
        string accountStatus,
        DateTimeOffset now,
        string? employeeId = null)
    {
        FullName = fullName.Trim();
        Email = NormalizeEmail(email);
        Phone = phone;
        UnmaskedPhone = phone;
        AccountStatus = accountStatus;
        EmployeeId = NormalizeOptional(employeeId);
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

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
