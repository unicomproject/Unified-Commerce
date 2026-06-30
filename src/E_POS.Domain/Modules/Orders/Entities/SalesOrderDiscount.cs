using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Orders.Entities;

public class SalesOrderDiscount : AuditableEntity
{
    public int ApplicationSequence { get; protected set; }
    public decimal DiscountAmount { get; protected set; }
    public Guid? SalesOrderId { get; protected set; }
    public Guid? SalesOrderLineId { get; protected set; }
}
