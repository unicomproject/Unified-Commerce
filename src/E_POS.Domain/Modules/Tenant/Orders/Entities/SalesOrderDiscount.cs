using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Orders.Entities;

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
    public bool? ApprovalRequiredSnapshot { get; protected set; }
    public Guid? ApprovedByTenantUserId { get; protected set; }
    public DateTimeOffset? ApprovedAt { get; protected set; }
    public Guid? AppliedByTenantUserId { get; protected set; }
    public DateTimeOffset AppliedAt { get; protected set; }

    public static SalesOrderDiscount CreateForPosSale(
        Guid id,
        Guid tenantId,
        Guid salesOrderId,
        Guid? salesOrderLineId,
        Guid discountPolicyId,
        Guid discountTypeId,
        string discountTargetScope,
        string discountCodeSnapshot,
        string discountNameSnapshot,
        string calculationMethodSnapshot,
        decimal discountValue,
        decimal discountAmount,
        string? manualDiscountReason,
        Guid appliedByTenantUserId,
        DateTimeOffset now) =>
        CreateForPosSale(
            id,
            tenantId,
            salesOrderId,
            salesOrderLineId,
            discountPolicyId,
            discountTypeId,
            discountTargetScope,
            discountCodeSnapshot,
            discountNameSnapshot,
            calculationMethodSnapshot,
            discountValue,
            discountAmount,
            manualDiscountReason,
            null,
            null,
            null,
            appliedByTenantUserId,
            now);

    public static SalesOrderDiscount CreateForPosSale(
        Guid id,
        Guid tenantId,
        Guid salesOrderId,
        Guid? salesOrderLineId,
        Guid discountPolicyId,
        Guid discountTypeId,
        string discountTargetScope,
        string discountCodeSnapshot,
        string discountNameSnapshot,
        string calculationMethodSnapshot,
        decimal discountValue,
        decimal discountAmount,
        string? manualDiscountReason,
        bool? approvalRequiredSnapshot,
        Guid? approvedByTenantUserId,
        DateTimeOffset? approvedAt,
        Guid appliedByTenantUserId,
        DateTimeOffset now) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            SalesOrderId = salesOrderId,
            SalesOrderLineId = salesOrderLineId,
            DiscountPolicyId = discountPolicyId,
            DiscountTypeId = discountTypeId,
            DiscountTargetScope = discountTargetScope.Trim().ToUpperInvariant(),
            ApplicationSequence = 1,
            DiscountCodeSnapshot = discountCodeSnapshot.Trim(),
            DiscountNameSnapshot = discountNameSnapshot.Trim(),
            CalculationMethodSnapshot = calculationMethodSnapshot.Trim().ToUpperInvariant(),
            DiscountValue = discountValue,
            DiscountAmount = discountAmount,
            ManualDiscountReason = string.IsNullOrWhiteSpace(manualDiscountReason) ? null : manualDiscountReason.Trim(),
            ApprovalRequiredSnapshot = approvalRequiredSnapshot,
            ApprovedByTenantUserId = approvedByTenantUserId,
            ApprovedAt = approvedAt,
            AppliedByTenantUserId = appliedByTenantUserId,
            AppliedAt = now,
            CreatedAt = now
        };
}

