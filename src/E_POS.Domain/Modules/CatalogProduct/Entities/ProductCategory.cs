using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductCategory : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid CategoryId { get; protected set; }
    public bool IsPrimaryCategory { get; protected set; }
    public int SortOrder { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductCategory Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        Guid categoryId,
        bool isPrimaryCategory,
        int sortOrder,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductCategory
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            CategoryId = categoryId,
            IsPrimaryCategory = isPrimaryCategory,
            SortOrder = sortOrder,
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
