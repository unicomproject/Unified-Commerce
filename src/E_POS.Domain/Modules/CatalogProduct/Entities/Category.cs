using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class Category : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string CategoryCode { get; protected set; } = string.Empty;
    public Guid? ParentCategoryId { get; protected set; }
    public int SortOrder { get; protected set; }

    public static Category Create(Guid id, Guid tenantId, string categoryCode, string name, string status, Guid? parentCategoryId, int sortOrder, DateTimeOffset now)
    {
        return new Category
        {
            Id = id,
            TenantId = tenantId,
            CategoryCode = CategoryConstants.NormalizeCode(categoryCode),
            Name = name.Trim(),
            Status = CategoryConstants.NormalizeStatus(status),
            ParentCategoryId = parentCategoryId,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(string categoryCode, string name, string status, Guid? parentCategoryId, int sortOrder, DateTimeOffset now)
    {
        CategoryCode = CategoryConstants.NormalizeCode(categoryCode);
        Name = name.Trim();
        Status = CategoryConstants.NormalizeStatus(status);
        ParentCategoryId = parentCategoryId;
        SortOrder = sortOrder;
        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        Status = CategoryConstants.DeletedStatus;
        UpdatedAt = now;
    }
}