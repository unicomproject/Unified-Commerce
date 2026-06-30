using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Orders.Entities;

public class SalesOrderStatusHistory : AuditableEntity
{
    public Guid? SalesOrderId { get; protected set; }
    public int SequenceNumber { get; protected set; }
}
