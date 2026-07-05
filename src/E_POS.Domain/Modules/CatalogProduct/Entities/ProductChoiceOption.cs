using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ProductChoiceOption : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductChoiceGroupId { get; protected set; }
    public Guid ChoiceGroupId { get; protected set; }
    public Guid ChoiceOptionId { get; protected set; }
    public decimal? PriceAdjustmentOverride { get; protected set; }
    public bool IsDefaultOption { get; protected set; }
    public bool IsAvailable { get; protected set; }
    public int? SortOrderOverride { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductChoiceOption Create(
        Guid id,
        Guid tenantId,
        Guid productChoiceGroupId,
        Guid choiceGroupId,
        Guid choiceOptionId,
        decimal? priceAdjustmentOverride,
        bool isDefaultOption,
        bool isAvailable,
        int? sortOrderOverride,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductChoiceOption
        {
            Id = id,
            TenantId = tenantId,
            ProductChoiceGroupId = productChoiceGroupId,
            ChoiceGroupId = choiceGroupId,
            ChoiceOptionId = choiceOptionId,
            PriceAdjustmentOverride = priceAdjustmentOverride,
            IsDefaultOption = isDefaultOption,
            IsAvailable = isAvailable,
            SortOrderOverride = sortOrderOverride,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
