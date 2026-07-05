using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.POSOperations.Entities;

public class TillSessionPaymentSummary : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid TillSessionSummaryId { get; protected set; }
    public Guid PaymentMethodId { get; protected set; }
    public decimal SalesAmount { get; protected set; }
    public decimal RefundAmount { get; protected set; }
    public decimal NetAmount { get; protected set; }
    public int TransactionCount { get; protected set; }
}
