using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Orders.Entities;

public class SalesOrderLineStatusHistory : AuditableEntity
{
    public Guid? SalesOrderLineId { get; protected set; }
    public int SequenceNumber { get; protected set; }
}
