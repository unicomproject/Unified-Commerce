using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Orders.Entities;

public class SalesOrderLineOption : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesOrderLineId { get; protected set; }
    public Guid? ProductChoiceGroupId { get; protected set; }
    public Guid? ProductChoiceOptionId { get; protected set; }
    public string ChoiceGroupNameSnapshot { get; protected set; } = string.Empty;
    public string ChoiceOptionNameSnapshot { get; protected set; } = string.Empty;
    public decimal Quantity { get; protected set; }
    public decimal UnitPriceAdjustment { get; protected set; }
    public decimal TotalPriceAdjustment { get; protected set; }
    public int SortOrder { get; protected set; }
}
