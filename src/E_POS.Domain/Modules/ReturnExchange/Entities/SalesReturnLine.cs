using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ReturnExchange.Entities;

public class SalesReturnLine : AuditableEntity
{
    public decimal RequestedQuantity { get; protected set; }
    public Guid? SalesOrderLineId { get; protected set; }
    public Guid? SalesReturnId { get; protected set; }
}
