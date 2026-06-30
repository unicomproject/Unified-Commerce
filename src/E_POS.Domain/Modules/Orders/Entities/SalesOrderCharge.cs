using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Orders.Entities;

public class SalesOrderCharge : AuditableEntity
{
    public decimal ChargeAmount { get; protected set; }
    public Guid? SalesOrderId { get; protected set; }
    public Guid? SalesOrderLineId { get; protected set; }
}
