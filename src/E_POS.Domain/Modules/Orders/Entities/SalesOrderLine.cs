using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Orders.Entities;

public class SalesOrderLine : AuditableEntity
{
    public int LineNumber { get; protected set; }
    public decimal LineTotalAmount { get; protected set; }
    public decimal Quantity { get; protected set; }
    public Guid? SalesOrderId { get; protected set; }
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid UomId { get; protected set; }
    public Guid? PriceListItemId { get; protected set; }
    public string? SkuSnapshot { get; protected set; }
    public string ProductNameSnapshot { get; protected set; } = string.Empty;
    public string? VariantNameSnapshot { get; protected set; }
    public string UomCodeSnapshot { get; protected set; } = string.Empty;
    public string UomNameSnapshot { get; protected set; } = string.Empty;
    public string ProductTypeSnapshot { get; protected set; } = string.Empty;
    public string ProductStructureSnapshot { get; protected set; } = string.Empty;
    public decimal FulfilledQuantity { get; protected set; }
    public decimal CancelledQuantity { get; protected set; }
    public decimal ReturnedQuantity { get; protected set; }
    public decimal OriginalUnitPrice { get; protected set; }
    public decimal UnitPrice { get; protected set; }
    public decimal LineSubtotalAmount { get; protected set; }
    public decimal LineDiscountAmount { get; protected set; }
    public decimal LineTaxAmount { get; protected set; }
    public string LineStatus { get; protected set; } = string.Empty;
}
