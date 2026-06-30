using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Refund.Entities;

public class SalesRefundLine : AuditableEntity
{
    public decimal Amount { get; protected set; }
    public Guid SalesRefundId { get; protected set; }
    public Guid SalesReturnLineId { get; protected set; }
}
