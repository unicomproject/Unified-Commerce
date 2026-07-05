using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductAttributeOption : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid AttributeDefinitionId { get; protected set; }
    public string OptionCode { get; protected set; } = string.Empty;
    public string OptionLabel { get; protected set; } = string.Empty;
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductAttributeOption Create(
        Guid id,
        Guid tenantId,
        Guid attributeDefinitionId,
        string optionCode,
        string optionLabel,
        int sortOrder,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductAttributeOption
        {
            Id = id,
            TenantId = tenantId,
            AttributeDefinitionId = attributeDefinitionId,
            OptionCode = optionCode.Trim().ToUpperInvariant(),
            OptionLabel = optionLabel.Trim(),
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
