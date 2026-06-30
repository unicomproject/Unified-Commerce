using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class Category : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string CategoryCode { get; protected set; } = string.Empty;
    public Guid ParentCategoryId { get; protected set; }
    public int SortOrder { get; protected set; }
}
