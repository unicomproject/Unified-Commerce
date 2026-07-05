using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ComboGroupItem : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ComboGroupId { get; protected set; }
    public Guid ItemProductId { get; protected set; }
    public Guid? ItemVariantId { get; protected set; }
    public Guid ItemUomId { get; protected set; }
    public decimal Quantity { get; protected set; }
    public decimal BasePriceAdjustment { get; protected set; }
    public bool IsDefaultItem { get; protected set; }
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ComboGroupItem Create(
        Guid id,
        Guid tenantId,
        Guid comboGroupId,
        Guid itemProductId,
        Guid? itemVariantId,
        Guid itemUomId,
        decimal quantity,
        decimal basePriceAdjustment,
        bool isDefaultItem,
        int sortOrder,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ComboGroupItem
        {
            Id = id,
            TenantId = tenantId,
            ComboGroupId = comboGroupId,
            ItemProductId = itemProductId,
            ItemVariantId = itemVariantId,
            ItemUomId = itemUomId,
            Quantity = quantity,
            BasePriceAdjustment = basePriceAdjustment,
            IsDefaultItem = isDefaultItem,
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
