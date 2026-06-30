using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Orders.Entities;

public class SalesOrder : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid DocumentNumberSequenceId { get; protected set; }
    public string OrderNumber { get; protected set; } = string.Empty;
    public decimal PaidAmount { get; protected set; }
    public decimal TotalAmount { get; protected set; }
}
