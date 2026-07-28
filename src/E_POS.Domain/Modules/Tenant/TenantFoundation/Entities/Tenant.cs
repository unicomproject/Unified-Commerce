using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class Tenant : AuditableEntity
{
    public string TenantCode { get; protected set; } = string.Empty;
    public string TenantSlug { get; protected set; } = string.Empty;
    public string DisplayName { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string BaseCurrencyCode { get; protected set; } = string.Empty;
    public string DefaultTimezone { get; protected set; } = string.Empty;
    public string? DefaultLocale { get; protected set; }
    public string? OperatingMode { get; protected set; }
    public string? DataRegion { get; protected set; }
    public DateTimeOffset? ActivatedAt { get; protected set; }
    public DateTimeOffset? SuspendedAt { get; protected set; }
    public DateTimeOffset? ArchivedAt { get; protected set; }
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public static Tenant Create(
        Guid id,
        string tenantCode,
        string tenantSlug,
        string displayName,
        string status,
        string baseCurrencyCode,
        string defaultTimezone,
        string? dataRegion,
        Guid? createdByPlatformUserId,
        DateTimeOffset now,
        string? defaultLocale = null,
        string? operatingMode = null)
    {
        return new Tenant
        {
            Id = id,
            TenantCode = tenantCode.Trim(),
            TenantSlug = tenantSlug.Trim(),
            DisplayName = displayName.Trim(),
            Status = TenantStatusConstants.Normalize(status),
            BaseCurrencyCode = baseCurrencyCode.Trim(),
            DefaultTimezone = defaultTimezone.Trim(),
            DefaultLocale = NormalizeOptional(defaultLocale),
            OperatingMode = NormalizeOptional(operatingMode),
            DataRegion = dataRegion?.Trim(),
            CreatedByPlatformUserId = createdByPlatformUserId,
            UpdatedByPlatformUserId = createdByPlatformUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDetails(
        string displayName,
        string defaultTimezone,
        string? dataRegion,
        Guid? updatedBy,
        DateTimeOffset now,
        string? defaultLocale = null,
        string? operatingMode = null,
        bool updateLocale = false,
        bool updateOperatingMode = false)
    {
        DisplayName = displayName.Trim();
        DefaultTimezone = defaultTimezone.Trim();
        DataRegion = dataRegion?.Trim();
        if (updateLocale)
        {
            DefaultLocale = NormalizeOptional(defaultLocale);
        }

        if (updateOperatingMode)
        {
            OperatingMode = NormalizeOptional(operatingMode);
        }

        UpdatedByPlatformUserId = updatedBy;
        UpdatedAt = now;
    }

    public void UpdateAudit(Guid? updatedBy, DateTimeOffset now)
    {
        UpdatedByPlatformUserId = updatedBy;
        UpdatedAt = now;
    }

    public void MarkPendingActivation(Guid? updatedBy, DateTimeOffset now)
    {
        if (!string.Equals(Status, TenantStatusConstants.PendingPayment, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Tenant can only move to pending_activation from pending_payment.");
        }

        Status = TenantStatusConstants.PendingActivation;
        UpdatedByPlatformUserId = updatedBy;
        UpdatedAt = now;
    }

    public void Activate(Guid? updatedBy, DateTimeOffset now)
    {
        if (!TenantLifecycleRules.CanActivate(Status))
        {
            throw new InvalidOperationException(
                $"Tenant cannot be activated from status '{Status}'.");
        }

        Status = TenantStatusConstants.Active;
        ActivatedAt = now;
        SuspendedAt = null;
        UpdatedByPlatformUserId = updatedBy;
        UpdatedAt = now;
    }

    public void Suspend(Guid? updatedBy, DateTimeOffset now)
    {
        if (!TenantLifecycleRules.CanSuspend(Status))
        {
            throw new InvalidOperationException(
                $"Tenant cannot be suspended from status '{Status}'.");
        }

        Status = TenantStatusConstants.Suspended;
        SuspendedAt = now;
        UpdatedByPlatformUserId = updatedBy;
        UpdatedAt = now;
    }

    public void Cancel(Guid? updatedBy, DateTimeOffset now)
    {
        Status = TenantStatusConstants.Cancelled;
        UpdatedByPlatformUserId = updatedBy;
        UpdatedAt = now;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
