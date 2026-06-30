using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Orders.Entities;

public class SalesOrderTax : AuditableEntity
{
    public Guid? SalesOrderId { get; protected set; }
    public Guid? SalesOrderLineId { get; protected set; }
    public decimal TaxAmount { get; protected set; }
    public decimal TaxRatePercent { get; protected set; }
}
