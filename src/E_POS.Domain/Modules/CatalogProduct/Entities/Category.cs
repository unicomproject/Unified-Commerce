using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class Category : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid DepartmentId { get; protected set; }
    public Guid? ParentCategoryId { get; protected set; }
    public string CategoryCode { get; protected set; } = string.Empty;
    public string CategoryName { get; protected set; } = string.Empty;
    public string CategorySlug { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static Category Create(
        Guid id, 
        Guid tenantId, 
        Guid departmentId,
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
            DepartmentId = departmentId,
            ParentCategoryId = parentCategoryId,
            CategoryCode = categoryCode.Trim().ToUpperInvariant(),
            CategoryName = categoryName.Trim(),
            CategorySlug = categorySlug.Trim().ToLowerInvariant(),
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
        Guid departmentId,
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
        DepartmentId = departmentId;
        ParentCategoryId = parentCategoryId;
        CategoryCode = categoryCode.Trim().ToUpperInvariant();
        CategoryName = categoryName.Trim();
        CategorySlug = categorySlug.Trim().ToLowerInvariant();
        Description = description?.Trim();
        SortOrder = sortOrder;
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