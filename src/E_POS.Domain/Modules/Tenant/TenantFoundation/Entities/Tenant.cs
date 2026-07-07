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
        DateTimeOffset now)
    {
        return new Tenant
        {
            Id = id,
            TenantCode = tenantCode.Trim(),
            TenantSlug = tenantSlug.Trim(),
            DisplayName = displayName.Trim(),
            Status = status,
            BaseCurrencyCode = baseCurrencyCode.Trim(),
            DefaultTimezone = defaultTimezone.Trim(),
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
        DateTimeOffset now)
    {
        DisplayName = displayName.Trim();
        DefaultTimezone = defaultTimezone.Trim();
        DataRegion = dataRegion?.Trim();
        UpdatedByPlatformUserId = updatedBy;
        UpdatedAt = now;
    }

    public void UpdateAudit(Guid? updatedBy, DateTimeOffset now)
    {
        UpdatedByPlatformUserId = updatedBy;
        UpdatedAt = now;
    }

    public void Activate(Guid? updatedBy, DateTimeOffset now)
    {
        Status = TenantStatusConstants.Active;
        ActivatedAt = now;
        UpdatedByPlatformUserId = updatedBy;
        UpdatedAt = now;
    }

    public void Suspend(Guid? updatedBy, DateTimeOffset now)
    {
        Status = TenantStatusConstants.Suspended;
        SuspendedAt = now;
        UpdatedByPlatformUserId = updatedBy;
        UpdatedAt = now;
    }
}
