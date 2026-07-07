using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.POSOperations.Entities;

public class TillSessionSummary : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid TillId { get; protected set; }
    public Guid TillSessionId { get; protected set; }
    public Guid CashierTenantUserId { get; protected set; }
    public DateTimeOffset SessionOpenedAt { get; protected set; }
    public DateTimeOffset? SessionClosedAt { get; protected set; }
    public decimal OpeningCashAmount { get; protected set; }
    public decimal ExpectedCashAmount { get; protected set; }
    public decimal CountedCashAmount { get; protected set; }
    public decimal CashDifferenceAmount { get; protected set; }
    public decimal GrossSalesAmount { get; protected set; }
    public decimal DiscountAmount { get; protected set; }
    public decimal TaxAmount { get; protected set; }
    public decimal ChargeAmount { get; protected set; }
    public decimal NetSalesAmount { get; protected set; }
    public decimal RefundAmount { get; protected set; }
    public decimal VoidAmount { get; protected set; }
    public int OrderCount { get; protected set; }
    public int RefundCount { get; protected set; }
    public int VoidCount { get; protected set; }
    public string SummaryStatus { get; protected set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; protected set; }
    public DateTimeOffset? ApprovedAt { get; protected set; }
    public Guid? ApprovedByTenantUserId { get; protected set; }
}

