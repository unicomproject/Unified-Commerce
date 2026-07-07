using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductAttributeValueOption : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductAttributeValueId { get; protected set; }
    public Guid AttributeDefinitionId { get; protected set; }
    public Guid AttributeOptionId { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }

    public static ProductAttributeValueOption Create(
        Guid id,
        Guid tenantId,
        Guid productAttributeValueId,
        Guid attributeDefinitionId,
        Guid attributeOptionId,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductAttributeValueOption
        {
            Id = id,
            TenantId = tenantId,
            ProductAttributeValueId = productAttributeValueId,
            AttributeDefinitionId = attributeDefinitionId,
            AttributeOptionId = attributeOptionId,
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

