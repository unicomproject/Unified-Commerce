using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductOption : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? SourceOptionTemplateId { get; protected set; }
    public string OptionCode { get; protected set; } = string.Empty;
    public string OptionName { get; protected set; } = string.Empty;
    public string OptionType { get; protected set; } = string.Empty;
    public string InputType { get; protected set; } = string.Empty;
    public bool IsRequired { get; protected set; } = true;
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductOption Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        Guid? sourceOptionTemplateId,
        string optionCode,
        string optionName,
        string optionType,
        string inputType,
        bool isRequired,
        int sortOrder,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductOption
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            SourceOptionTemplateId = sourceOptionTemplateId,
            OptionCode = optionCode.Trim(),
            OptionName = optionName.Trim(),
            OptionType = optionType.Trim().ToUpperInvariant(),
            InputType = inputType.Trim().ToUpperInvariant(),
            IsRequired = isRequired,
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
