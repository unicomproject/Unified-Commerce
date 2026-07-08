using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Discount.Entities;

public class ExpiryDiscountRule : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid DiscountPolicyId { get; protected set; }
    public string RuleCode { get; protected set; } = string.Empty;
    public string RuleName { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public bool RequireManagerApproval { get; protected set; }
    public bool IsAutoApply { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ExpiryDiscountRule Create(
        Guid id,
        Guid tenantId,
        Guid discountPolicyId,
        string ruleCode,
        string ruleName,
        string? description,
        bool requireManagerApproval,
        bool isAutoApply,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ExpiryDiscountRule
        {
            Id = id,
            TenantId = tenantId,
            DiscountPolicyId = discountPolicyId,
            RuleCode = ruleCode.Trim().ToUpperInvariant(),
            RuleName = ruleName.Trim(),
            Description = description?.Trim(),
            RequireManagerApproval = requireManagerApproval,
            IsAutoApply = isAutoApply,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string ruleCode,
        string ruleName,
        string? description,
        bool requireManagerApproval,
        bool isAutoApply,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        RuleCode = ruleCode.Trim().ToUpperInvariant();
        RuleName = ruleName.Trim();
        Description = description?.Trim();
        RequireManagerApproval = requireManagerApproval;
        IsAutoApply = isAutoApply;
        Status = status.Trim().ToUpperInvariant();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = "DELETED";
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}
