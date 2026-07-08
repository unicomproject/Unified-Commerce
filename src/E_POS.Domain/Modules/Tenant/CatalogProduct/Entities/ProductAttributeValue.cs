using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductAttributeValue : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid AttributeDefinitionId { get; protected set; }
    public string? AttributeValueText { get; protected set; }
    public decimal? AttributeValueNumber { get; protected set; }
    public bool? AttributeValueBoolean { get; protected set; }
    public DateOnly? AttributeValueDate { get; protected set; }
    public Guid? AttributeValueUomId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductAttributeValue Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        Guid? productVariantId,
        Guid attributeDefinitionId,
        string? attributeValueText,
        decimal? attributeValueNumber,
        bool? attributeValueBoolean,
        DateOnly? attributeValueDate,
        Guid? attributeValueUomId,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductAttributeValue
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            AttributeDefinitionId = attributeDefinitionId,
            AttributeValueText = attributeValueText?.Trim(),
            AttributeValueNumber = attributeValueNumber,
            AttributeValueBoolean = attributeValueBoolean,
            AttributeValueDate = attributeValueDate,
            AttributeValueUomId = attributeValueUomId,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

