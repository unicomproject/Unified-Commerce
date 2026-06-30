using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Refund.Entities;

public class SalesRefund : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string RefundNumber { get; protected set; } = string.Empty;
    public decimal RefundedAmount { get; protected set; }
    public decimal RequestedAmount { get; protected set; }
    public Guid? SalesOrderId { get; protected set; }
    public Guid? SalesReturnId { get; protected set; }
}
