using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductAttributeDefinition : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string AttributeKey { get; protected set; } = string.Empty;
    public string AttributeName { get; protected set; } = string.Empty;
    public string AttributeType { get; protected set; } = string.Empty;
    public Guid? DefaultUomId { get; protected set; }
    public bool IsFilterable { get; protected set; }
    public bool IsSearchable { get; protected set; }
    public bool IsRequired { get; protected set; }
    public string AppliesTo { get; protected set; } = string.Empty;
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductAttributeDefinition Create(
        Guid id,
        Guid tenantId,
        string attributeKey,
        string attributeName,
        string attributeType,
        Guid? defaultUomId,
        bool isFilterable,
        bool isSearchable,
        bool isRequired,
        string appliesTo,
        int sortOrder,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductAttributeDefinition
        {
            Id = id,
            TenantId = tenantId,
            AttributeKey = attributeKey.Trim().ToLowerInvariant(),
            AttributeName = attributeName.Trim(),
            AttributeType = attributeType.Trim().ToUpperInvariant(),
            DefaultUomId = defaultUomId,
            IsFilterable = isFilterable,
            IsSearchable = isSearchable,
            IsRequired = isRequired,
            AppliesTo = appliesTo.Trim().ToUpperInvariant(),
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
