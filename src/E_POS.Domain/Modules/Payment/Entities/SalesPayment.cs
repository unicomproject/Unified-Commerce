using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Payment.Entities;

public class SalesPayment : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public decimal PaidAmount { get; protected set; }
    public Guid PaymentMethodId { get; protected set; }
    public string PaymentNumber { get; protected set; } = string.Empty;
    public decimal RequestedAmount { get; protected set; }
    public Guid? SalesOrderId { get; protected set; }
}
