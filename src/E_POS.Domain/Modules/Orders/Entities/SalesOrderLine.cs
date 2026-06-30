using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Orders.Entities;

public class SalesOrderLine : AuditableEntity
{
    public string LineNumber { get; protected set; } = string.Empty;
    public decimal LineTotalAmount { get; protected set; }
    public decimal Quantity { get; protected set; }
    public Guid? SalesOrderId { get; protected set; }
}
