using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Discount.Entities;

public sealed class PosDiscountApplication : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid DiscountPolicyId { get; private set; }
    public Guid DiscountTypeId { get; private set; }
    public Guid OutletId { get; private set; }
    public Guid TillId { get; private set; }
    public Guid TillSessionId { get; private set; }
    public Guid PosDeviceId { get; private set; }
    public Guid RequestedByTenantUserId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? TargetProductVariantId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string DiscountSource { get; private set; } = string.Empty;
    public string DiscountScope { get; private set; } = string.Empty;
    public string PolicyCodeSnapshot { get; private set; } = string.Empty;
    public string PolicyNameSnapshot { get; private set; } = string.Empty;
    public string CalculationMethodSnapshot { get; private set; } = string.Empty;
    public decimal RequestedValue { get; private set; }
    public decimal CashierLimitSnapshot { get; private set; }
    public decimal AbsoluteLimitSnapshot { get; private set; }
    public decimal CartSubtotalSnapshot { get; private set; }
    public decimal EligibleSubtotalSnapshot { get; private set; }
    public decimal DiscountAmountSnapshot { get; private set; }
    public decimal TotalAfterDiscountSnapshot { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;
    public string CartSnapshotJson { get; private set; } = string.Empty;
    public string CartHash { get; private set; } = string.Empty;
    public string? RequestReason { get; private set; }
    public string ApplicationStatus { get; private set; } = string.Empty;
    public bool RequiresManagerApproval { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid? DecidedByTenantUserId { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? DecisionNote { get; private set; }
    public Guid? SalesOrderId { get; private set; }
    public DateTimeOffset? AppliedAt { get; private set; }

    public static PosDiscountApplication Create(
        Guid id,
        Guid tenantId,
        Guid discountPolicyId,
        Guid discountTypeId,
        Guid outletId,
        Guid tillId,
        Guid tillSessionId,
        Guid posDeviceId,
        Guid requestedByTenantUserId,
        Guid? customerId,
        Guid? targetProductVariantId,
        string idempotencyKey,
        string discountSource,
        string discountScope,
        string policyCodeSnapshot,
        string policyNameSnapshot,
        string calculationMethodSnapshot,
        decimal requestedValue,
        decimal cashierLimitSnapshot,
        decimal absoluteLimitSnapshot,
        decimal cartSubtotalSnapshot,
        decimal eligibleSubtotalSnapshot,
        decimal discountAmountSnapshot,
        decimal totalAfterDiscountSnapshot,
        string currencyCode,
        string cartSnapshotJson,
        string cartHash,
        string? requestReason,
        bool requiresManagerApproval,
        DateTimeOffset expiresAt,
        DateTimeOffset now) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            DiscountPolicyId = discountPolicyId,
            DiscountTypeId = discountTypeId,
            OutletId = outletId,
            TillId = tillId,
            TillSessionId = tillSessionId,
            PosDeviceId = posDeviceId,
            RequestedByTenantUserId = requestedByTenantUserId,
            CustomerId = customerId,
            TargetProductVariantId = targetProductVariantId,
            IdempotencyKey = idempotencyKey.Trim(),
            DiscountSource = discountSource.Trim().ToUpperInvariant(),
            DiscountScope = discountScope.Trim().ToUpperInvariant(),
            PolicyCodeSnapshot = policyCodeSnapshot.Trim(),
            PolicyNameSnapshot = policyNameSnapshot.Trim(),
            CalculationMethodSnapshot = calculationMethodSnapshot.Trim().ToUpperInvariant(),
            RequestedValue = requestedValue,
            CashierLimitSnapshot = cashierLimitSnapshot,
            AbsoluteLimitSnapshot = absoluteLimitSnapshot,
            CartSubtotalSnapshot = cartSubtotalSnapshot,
            EligibleSubtotalSnapshot = eligibleSubtotalSnapshot,
            DiscountAmountSnapshot = discountAmountSnapshot,
            TotalAfterDiscountSnapshot = totalAfterDiscountSnapshot,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            CartSnapshotJson = cartSnapshotJson,
            CartHash = cartHash,
            RequestReason = string.IsNullOrWhiteSpace(requestReason) ? null : requestReason.Trim(),
            RequiresManagerApproval = requiresManagerApproval,
            ApplicationStatus = requiresManagerApproval ? "PENDING_APPROVAL" : "APPROVED",
            RequestedAt = now,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };

    public bool CanBeUsed(DateTimeOffset now) =>
        ApplicationStatus == "APPROVED" && AppliedAt is null && ExpiresAt > now;

    public void Approve(Guid managerId, string? note, DateTimeOffset now)
    {
        if (ApplicationStatus != "PENDING_APPROVAL")
        {
            throw new InvalidOperationException("Only pending discount applications can be approved.");
        }

        ApplicationStatus = "APPROVED";
        DecidedByTenantUserId = managerId;
        DecidedAt = now;
        DecisionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        UpdatedAt = now;
    }

    public void Reject(Guid managerId, string? note, DateTimeOffset now)
    {
        if (ApplicationStatus != "PENDING_APPROVAL")
        {
            throw new InvalidOperationException("Only pending discount applications can be rejected.");
        }

        ApplicationStatus = "REJECTED";
        DecidedByTenantUserId = managerId;
        DecidedAt = now;
        DecisionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        UpdatedAt = now;
    }

    public void Cancel(string? reason, DateTimeOffset now)
    {
        if (ApplicationStatus == "CANCELLED") return;
        if (ApplicationStatus is "APPLIED" or "REJECTED" or "EXPIRED")
            throw new InvalidOperationException("Finalized discount application cannot be cancelled.");
        ApplicationStatus = "CANCELLED";
        DecisionNote = string.IsNullOrWhiteSpace(reason) ? DecisionNote : reason.Trim();
        UpdatedAt = now;
    }

    public void MarkApplied(Guid salesOrderId, DateTimeOffset now)
    {
        if (!CanBeUsed(now))
        {
            throw new InvalidOperationException("Discount application is not approved or has expired.");
        }

        ApplicationStatus = "APPLIED";
        SalesOrderId = salesOrderId;
        AppliedAt = now;
        UpdatedAt = now;
    }
}
