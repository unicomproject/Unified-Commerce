using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Orders.Entities;

public class SalesOrderDiscount : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesOrderId { get; protected set; }
    public Guid? SalesOrderLineId { get; protected set; }
    public Guid? DiscountPolicyId { get; protected set; }
    public Guid DiscountTypeId { get; protected set; }
    public string DiscountTargetScope { get; protected set; } = string.Empty;
    public int ApplicationSequence { get; protected set; }
    public string? DiscountCodeSnapshot { get; protected set; }
    public string DiscountNameSnapshot { get; protected set; } = string.Empty;
    public string CalculationMethodSnapshot { get; protected set; } = string.Empty;
    public decimal DiscountValue { get; protected set; }
    public decimal DiscountAmount { get; protected set; }
    public string? ManualDiscountReason { get; protected set; }
    public Guid? AppliedByTenantUserId { get; protected set; }
    public DateTimeOffset AppliedAt { get; protected set; }
}
