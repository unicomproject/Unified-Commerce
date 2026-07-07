using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.HardwareCash.Entities;

public class CashReconciliation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid TillSessionId { get; protected set; }
    public string ReconciliationNumber { get; protected set; } = string.Empty;
    public decimal ExpectedCashAmount { get; protected set; }
    public decimal CountedCashAmount { get; protected set; }
    public decimal DifferenceAmount { get; protected set; }
    public string CurrencyCode { get; protected set; } = string.Empty;
    public string ReconciliationStatus { get; protected set; } = string.Empty;
    public string? DifferenceReason { get; protected set; }
    public string? CalculationDetailsJson { get; protected set; }
    public Guid? SubmittedByTenantUserId { get; protected set; }
    public DateTimeOffset? SubmittedAt { get; protected set; }
    public Guid? ApprovedByTenantUserId { get; protected set; }
    public DateTimeOffset? ApprovedAt { get; protected set; }
    public string? ApprovalNote { get; protected set; }
    
    // Ignores CreatedBy/UpdatedBy inherited from AuditableEntity since ERD doesn't mention them.

    public static CashReconciliation Create(
        Guid id,
        Guid tenantId,
        Guid tillSessionId,
        string reconciliationNumber,
        decimal expectedCashAmount,
        decimal countedCashAmount,
        decimal differenceAmount,
        string currencyCode,
        string? differenceReason,
        string? calculationDetailsJson,
        DateTimeOffset now)
    {
        return new CashReconciliation
        {
            Id = id,
            TenantId = tenantId,
            TillSessionId = tillSessionId,
            ReconciliationNumber = reconciliationNumber.Trim().ToUpperInvariant(),
            ExpectedCashAmount = expectedCashAmount,
            CountedCashAmount = countedCashAmount,
            DifferenceAmount = differenceAmount,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            ReconciliationStatus = "DRAFT",
            DifferenceReason = differenceReason?.Trim(),
            CalculationDetailsJson = calculationDetailsJson,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Submit(Guid submittedByTenantUserId, DateTimeOffset now)
    {
        ReconciliationStatus = "SUBMITTED";
        SubmittedByTenantUserId = submittedByTenantUserId;
        SubmittedAt = now;
        UpdatedAt = now;
    }

    public void Approve(Guid approvedByTenantUserId, string? approvalNote, DateTimeOffset now)
    {
        ReconciliationStatus = "APPROVED";
        ApprovedByTenantUserId = approvedByTenantUserId;
        ApprovalNote = approvalNote?.Trim();
        ApprovedAt = now;
        UpdatedAt = now;
    }

    public void Reject(Guid rejectedByTenantUserId, string? rejectionNote, DateTimeOffset now)
    {
        ReconciliationStatus = "REJECTED";
        ApprovedByTenantUserId = rejectedByTenantUserId; // using same field for the decider
        ApprovalNote = rejectionNote?.Trim();
        ApprovedAt = now;
        UpdatedAt = now;
    }
}

