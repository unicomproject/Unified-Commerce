using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class Brand : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string BrandCode { get; protected set; } = string.Empty;
    public string BrandName { get; protected set; } = string.Empty;
    public string BrandSlug { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public int SortOrder { get; protected set; }
    public Guid? LogoMediaAssetId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
    public long RowVersion { get; protected set; } = 1;

    public static Brand Create(
        Guid id,
        Guid tenantId,
        string brandCode,
        string brandName,
        string brandSlug,
        string? description,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now,
        int sortOrder = 0)
    {
        return new Brand
        {
            Id = id,
            TenantId = tenantId,
            BrandCode = brandCode.Trim().ToUpperInvariant(),
            BrandName = brandName.Trim(),
            BrandSlug = brandSlug.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string brandCode,
        string brandName,
        string brandSlug,
        string? description,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now,
        int sortOrder = 0)
    {
        BrandCode = brandCode.Trim().ToUpperInvariant();
        BrandName = brandName.Trim();
        BrandSlug = brandSlug.Trim().ToLowerInvariant();
        Description = description?.Trim();
        SortOrder = sortOrder;
        Status = status.Trim().ToUpperInvariant();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
        RowVersion++;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = "DELETED";
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
        RowVersion++;
    }

    public void UpdateLogo(
        Guid? logoMediaAssetId,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        LogoMediaAssetId = logoMediaAssetId;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void IncrementRowVersion() => RowVersion++;
    public static Brand Create(
        Guid id,
        Guid tenantId,
        string brandCode,
        string brandName,
        string brandSlug,
        string? description,
        string? logoUrl,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now,
        int sortOrder = 0)
    {
        return Create(id, tenantId, brandCode, brandName, brandSlug, description, status, createdByTenantUserId, now, sortOrder);
    }

    public void UpdateLogo(
        string? logoUrl,
        Guid? logoMediaAssetId,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        UpdateLogo(logoMediaAssetId, updatedByTenantUserId, now);
    }
}
