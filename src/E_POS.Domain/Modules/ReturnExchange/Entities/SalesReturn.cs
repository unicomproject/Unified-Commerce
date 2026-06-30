using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ReturnExchange.Entities;

public class SalesReturn : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string ReturnNumber { get; protected set; } = string.Empty;
    public Guid? SalesOrderId { get; protected set; }
    public decimal TotalRefundAmount { get; protected set; }
    public decimal TotalReturnAmount { get; protected set; }
}
