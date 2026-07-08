using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.Refund.Entities;

public class SalesRefundLine : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesRefundId { get; protected set; }
    public Guid? SalesReturnLineId { get; protected set; }
    public string RefundLineType { get; protected set; } = string.Empty;
    public string DescriptionSnapshot { get; protected set; } = string.Empty;
    public decimal? Quantity { get; protected set; }
    public decimal Amount { get; protected set; }
}

