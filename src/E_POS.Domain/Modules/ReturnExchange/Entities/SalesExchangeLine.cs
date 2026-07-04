using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ReturnExchange.Entities;

public class SalesExchangeLine : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesExchangeId { get; protected set; }
    public Guid? SalesReturnLineId { get; protected set; }
    public Guid? ReplacementProductId { get; protected set; }
    public Guid? ReplacementProductVariantId { get; protected set; }
    public Guid? ReplacementSalesOrderLineId { get; protected set; }
    public decimal Quantity { get; protected set; }
    public decimal OriginalLineAmount { get; protected set; }
    public decimal ReplacementLineAmount { get; protected set; }
    public decimal NetDifferenceAmount { get; protected set; }
    public string ExchangeActionType { get; protected set; } = string.Empty;
}
