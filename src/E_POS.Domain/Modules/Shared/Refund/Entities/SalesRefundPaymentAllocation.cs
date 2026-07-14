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

    public static SalesRefundPaymentAllocation CreateCompleted(
        Guid id,
        Guid tenantId,
        Guid salesRefundId,
        Guid originalSalesPaymentId,
        Guid refundPaymentMethodId,
        decimal amount,
        string? externalReference,
        DateTimeOffset now) => new()
        {
            Id = id,
            TenantId = tenantId,
            SalesRefundId = salesRefundId,
            OriginalSalesPaymentId = originalSalesPaymentId,
            RefundPaymentMethodId = refundPaymentMethodId,
            AllocatedAmount = amount,
            AllocationStatus = "COMPLETED",
            ExternalReference = string.IsNullOrWhiteSpace(externalReference)
                ? null
                : externalReference.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
}

