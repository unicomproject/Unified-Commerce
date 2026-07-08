using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.Refund.Entities;

public class SalesRefundPaymentAllocation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesRefundId { get; protected set; }
    public Guid OriginalSalesPaymentId { get; protected set; }
    public Guid RefundPaymentMethodId { get; protected set; }
    public Guid? RefundTransactionId { get; protected set; }
    public decimal AllocatedAmount { get; protected set; }
    public string AllocationStatus { get; protected set; } = string.Empty;
    public string? ExternalReference { get; protected set; }
}

