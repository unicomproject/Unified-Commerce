using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ChoiceOption : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ChoiceGroupId { get; protected set; }
    public string OptionCode { get; protected set; } = string.Empty;
    public string OptionName { get; protected set; } = string.Empty;
    public decimal DefaultPriceAdjustment { get; protected set; }
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ChoiceOption Create(
        Guid id,
        Guid tenantId,
        Guid choiceGroupId,
        string optionCode,
        string optionName,
        decimal defaultPriceAdjustment,
        int sortOrder,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ChoiceOption
        {
            Id = id,
            TenantId = tenantId,
            ChoiceGroupId = choiceGroupId,
            OptionCode = optionCode.Trim(),
            OptionName = optionName.Trim(),
            DefaultPriceAdjustment = defaultPriceAdjustment,
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
