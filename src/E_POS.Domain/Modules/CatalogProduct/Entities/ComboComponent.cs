using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

public class ComboComponent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ComboDefinitionId { get; protected set; }
    public Guid ComponentProductId { get; protected set; }
    public Guid? ComponentVariantId { get; protected set; }
    public Guid ComponentUomId { get; protected set; }
    public decimal Quantity { get; protected set; }
    public decimal BasePriceAdjustment { get; protected set; }
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ComboComponent Create(
        Guid id,
        Guid tenantId,
        Guid comboDefinitionId,
        Guid componentProductId,
        Guid? componentVariantId,
        Guid componentUomId,
        decimal quantity,
        decimal basePriceAdjustment,
        int sortOrder,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ComboComponent
        {
            Id = id,
            TenantId = tenantId,
            ComboDefinitionId = comboDefinitionId,
            ComponentProductId = componentProductId,
            ComponentVariantId = componentVariantId,
            ComponentUomId = componentUomId,
            Quantity = quantity,
            BasePriceAdjustment = basePriceAdjustment,
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
