using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class Category : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? ParentCategoryId { get; protected set; }
    public string CategoryCode { get; protected set; } = string.Empty;
    public string CategoryName { get; protected set; } = string.Empty;
    public string CategorySlug { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public Guid? ImageMediaAssetId { get; protected set; }
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static Category Create(
        Guid id,
        Guid tenantId,
        Guid? parentCategoryId,
        string categoryCode,
        string categoryName,
        string categorySlug,
        string? description,
        int sortOrder,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new Category
        {
            Id = id,
            TenantId = tenantId,
            ParentCategoryId = parentCategoryId,
            CategoryCode = CategoryConstants.NormalizeCode(categoryCode),
            CategoryName = CategoryConstants.NormalizeName(categoryName),
            CategorySlug = CategoryConstants.NormalizeSlug(categorySlug),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            SortOrder = sortOrder,
            Status = CategoryConstants.NormalizeStatus(status),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        Guid? parentCategoryId,
        string categoryCode,
        string categoryName,
        string categorySlug,
        string? description,
        int sortOrder,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        ParentCategoryId = parentCategoryId;
        CategoryCode = CategoryConstants.NormalizeCode(categoryCode);
        CategoryName = CategoryConstants.NormalizeName(categoryName);
        CategorySlug = CategoryConstants.NormalizeSlug(categorySlug);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SortOrder = sortOrder;
        Status = CategoryConstants.NormalizeStatus(status);
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = CategoryConstants.DeletedStatus;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void UpdateImage(
        Guid? imageMediaAssetId,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        ImageMediaAssetId = imageMediaAssetId;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public static Category Create(
        Guid id,
        Guid tenantId,
        Guid? parentCategoryId,
        string categoryCode,
        string categoryName,
        string categorySlug,
        string? description,
        string? imageUrl,
        int sortOrder,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return Create(
            id,
            tenantId,
            parentCategoryId,
            categoryCode,
            categoryName,
            categorySlug,
            description,
            sortOrder,
            status,
            createdByTenantUserId,
            now);
    }

    public void UpdateImage(
        string? imageUrl,
        Guid? imageMediaAssetId,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        UpdateImage(imageMediaAssetId, updatedByTenantUserId, now);
    }
}
