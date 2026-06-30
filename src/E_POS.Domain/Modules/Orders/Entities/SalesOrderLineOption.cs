using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Orders.Entities;

public class SalesOrderLineOption : AuditableEntity
{
    public decimal Quantity { get; protected set; }
    public Guid? SalesOrderLineId { get; protected set; }
    public int SortOrder { get; protected set; }
}
