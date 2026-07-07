using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class Brand : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string BrandCode { get; protected set; } = string.Empty;
    public string BrandName { get; protected set; } = string.Empty;
    public string BrandSlug { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string? LogoUrl { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

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
        DateTimeOffset now)
    {
        return new Brand
        {
            Id = id,
            TenantId = tenantId,
            BrandCode = brandCode.Trim().ToUpperInvariant(),
            BrandName = brandName.Trim(),
            BrandSlug = brandSlug.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            LogoUrl = logoUrl?.Trim(),
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
        string? logoUrl,
        string status, 
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        BrandCode = brandCode.Trim().ToUpperInvariant();
        BrandName = brandName.Trim();
        BrandSlug = brandSlug.Trim().ToLowerInvariant();
        Description = description?.Trim();
        LogoUrl = logoUrl?.Trim();
        Status = status.Trim().ToUpperInvariant();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = "DELETED";
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}
