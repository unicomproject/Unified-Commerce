using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public class TenantFeatureEntitlement : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string EntitlementStatus { get; protected set; } = string.Empty;
    public Guid PlatformFeatureId { get; protected set; }
    public Guid FeatureId { get; protected set; }
    public string SourceType { get; protected set; } = "MANUAL";
    public Guid? SourceReferenceId { get; protected set; }
    public bool IsEnabled { get; protected set; } = true;
    public DateTimeOffset EffectiveFrom { get; protected set; }
    public DateTimeOffset? EffectiveUntil { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public Guid? RevokedByPlatformUserId { get; protected set; }
    public string? RevokedReason { get; protected set; }
    public Guid? CreatedByPlatformUserId { get; protected set; }
    public Guid? UpdatedByPlatformUserId { get; protected set; }

    public static TenantFeatureEntitlement Create(
        Guid id,
        Guid tenantId,
        Guid platformFeatureId,
        string entitlementStatus,
        DateTimeOffset createdAt)
    {
        var isEnabled = string.Equals(entitlementStatus, "ENABLED", StringComparison.OrdinalIgnoreCase);
        return Create(
            id,
            tenantId,
            platformFeatureId,
            entitlementStatus,
            TenantEntitlementSourceTypeConstants.Manual,
            sourceReferenceId: null,
            isEnabled,
            effectiveFrom: createdAt,
            effectiveUntil: null,
            createdByPlatformUserId: null,
            updatedByPlatformUserId: null,
            createdAt);
    }

    public static TenantFeatureEntitlement Create(
        Guid id,
        Guid tenantId,
        Guid platformFeatureId,
        string entitlementStatus,
        string sourceType,
        Guid? sourceReferenceId,
        bool isEnabled,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveUntil,
        Guid? createdByPlatformUserId,
        Guid? updatedByPlatformUserId,
        DateTimeOffset createdAt)
    {
        return new TenantFeatureEntitlement
        {
            Id = id,
            TenantId = tenantId,
            PlatformFeatureId = platformFeatureId,
            FeatureId = platformFeatureId,
            EntitlementStatus = entitlementStatus,
            SourceType = sourceType,
            SourceReferenceId = sourceReferenceId,
            IsEnabled = isEnabled,
            EffectiveFrom = effectiveFrom,
            EffectiveUntil = effectiveUntil,
            CreatedByPlatformUserId = createdByPlatformUserId,
            UpdatedByPlatformUserId = updatedByPlatformUserId,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void Enable(
        Guid platformFeatureId,
        DateTimeOffset now,
        Guid? updatedByPlatformUserId,
        string sourceType = TenantEntitlementSourceTypeConstants.Manual,
        Guid? sourceReferenceId = null)
    {
        PlatformFeatureId = platformFeatureId;
        FeatureId = platformFeatureId;
        EntitlementStatus = TenantEntitlementStatusConstants.Enabled;
        IsEnabled = true;
        SourceType = string.IsNullOrWhiteSpace(sourceType)
            ? TenantEntitlementSourceTypeConstants.Manual
            : sourceType.Trim().ToUpperInvariant();
        SourceReferenceId = sourceReferenceId;
        EffectiveFrom = now;
        EffectiveUntil = null;
        RevokedAt = null;
        RevokedByPlatformUserId = null;
        RevokedReason = null;
        UpdatedByPlatformUserId = updatedByPlatformUserId;
        UpdatedAt = now;
    }

    public void Disable(
        DateTimeOffset now,
        Guid? revokedByPlatformUserId,
        string revokedReason,
        Guid? updatedByPlatformUserId)
    {
        EntitlementStatus = TenantEntitlementStatusConstants.Disabled;
        IsEnabled = false;
        RevokedAt = now;
        RevokedByPlatformUserId = revokedByPlatformUserId;
        RevokedReason = string.IsNullOrWhiteSpace(revokedReason)
            ? null
            : revokedReason.Trim();
        UpdatedByPlatformUserId = updatedByPlatformUserId;
        UpdatedAt = now;
    }
}

