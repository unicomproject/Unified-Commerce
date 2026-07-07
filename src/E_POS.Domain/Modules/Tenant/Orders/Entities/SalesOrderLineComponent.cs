using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Orders.Entities;

public class SalesOrderLineComponent : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesOrderLineId { get; protected set; }
    public Guid ComboDefinitionId { get; protected set; }
    public Guid? ComboComponentId { get; protected set; }
    public Guid? ComboGroupItemId { get; protected set; }
    public string ItemSourceType { get; protected set; } = string.Empty;
    public Guid ItemProductId { get; protected set; }
    public Guid? ItemVariantId { get; protected set; }
    public Guid ItemUomId { get; protected set; }
    public string? ItemSkuSnapshot { get; protected set; }
    public string ItemNameSnapshot { get; protected set; } = string.Empty;
    public string? ItemVariantNameSnapshot { get; protected set; }
    public string ItemUomCodeSnapshot { get; protected set; } = string.Empty;
    public string ItemUomNameSnapshot { get; protected set; } = string.Empty;
    public decimal Quantity { get; protected set; }
    public decimal UnitPriceAdjustment { get; protected set; }
    public decimal TotalPriceAdjustment { get; protected set; }
    public int SortOrder { get; protected set; }
}

