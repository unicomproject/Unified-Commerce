using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.POSOperations.Entities;

public class Receipt : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid DocumentNumberSequenceId { get; protected set; }
    public string ReceiptNumber { get; protected set; } = string.Empty;
    public Guid? SalesOrderId { get; protected set; }
    public decimal TotalAmount { get; protected set; }
}
