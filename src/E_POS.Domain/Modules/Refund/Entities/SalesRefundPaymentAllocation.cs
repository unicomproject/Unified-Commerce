using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Refund.Entities;

public class SalesRefundPaymentAllocation : AuditableEntity
{
    public decimal AllocatedAmount { get; protected set; }
    public Guid OriginalSalesPaymentId { get; protected set; }
    public Guid SalesRefundId { get; protected set; }
}
