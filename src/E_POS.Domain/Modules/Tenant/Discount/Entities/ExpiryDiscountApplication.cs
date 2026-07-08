using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Discount.Entities;

public class ExpiryDiscountApplication : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ExpiryDiscountRuleId { get; protected set; }
    public Guid ExpiryDiscountRuleTierId { get; protected set; }
    public Guid ProductBatchId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public decimal DiscountPercent { get; protected set; }
    public string ApplicationSource { get; protected set; } = string.Empty;
    public string ApplicationStatus { get; protected set; } = string.Empty;
    public DateTimeOffset AppliedFrom { get; protected set; }
    public DateTimeOffset? AppliedUntil { get; protected set; }
    public Guid? ApprovedByTenantUserId { get; protected set; }
    public DateTimeOffset? ApprovedAt { get; protected set; }
    public string? ApprovalNote { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ExpiryDiscountApplication Create(
        Guid id,
        Guid tenantId,
        Guid expiryDiscountRuleId,
        Guid expiryDiscountRuleTierId,
        Guid productBatchId,
        Guid outletId,
        decimal discountPercent,
        string applicationSource,
        string applicationStatus,
        DateTimeOffset appliedFrom,
        DateTimeOffset? appliedUntil,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ExpiryDiscountApplication
        {
            Id = id,
            TenantId = tenantId,
            ExpiryDiscountRuleId = expiryDiscountRuleId,
            ExpiryDiscountRuleTierId = expiryDiscountRuleTierId,
            ProductBatchId = productBatchId,
            OutletId = outletId,
            DiscountPercent = discountPercent,
            ApplicationSource = applicationSource.Trim().ToUpperInvariant(),
            ApplicationStatus = applicationStatus.Trim().ToUpperInvariant(),
            AppliedFrom = appliedFrom,
            AppliedUntil = appliedUntil,
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Approve(Guid approvedByTenantUserId, string? approvalNote, DateTimeOffset now)
    {
        ApplicationStatus = "APPROVED";
        ApprovedByTenantUserId = approvedByTenantUserId;
        ApprovalNote = approvalNote?.Trim();
        ApprovedAt = now;
        UpdatedByTenantUserId = approvedByTenantUserId;
        UpdatedAt = now;
    }

    public void Reject(Guid updatedByTenantUserId, string? approvalNote, DateTimeOffset now)
    {
        ApplicationStatus = "REJECTED";
        ApprovedByTenantUserId = updatedByTenantUserId;
        ApprovalNote = approvalNote?.Trim();
        ApprovedAt = now;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        ApplicationStatus = "DELETED";
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}
