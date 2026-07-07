using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Discount.Entities;

public class DiscountPolicyCondition : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid DiscountPolicyId { get; protected set; }
    public int ConditionGroupNo { get; protected set; }
    public string GroupOperator { get; protected set; } = string.Empty;
    public string ConditionType { get; protected set; } = string.Empty;
    public string ConditionOperator { get; protected set; } = string.Empty;
    public string ConditionValueJson { get; protected set; } = string.Empty;
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static DiscountPolicyCondition Create(
        Guid id,
        Guid tenantId,
        Guid discountPolicyId,
        int conditionGroupNo,
        string groupOperator,
        string conditionType,
        string conditionOperator,
        string conditionValueJson,
        int sortOrder,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new DiscountPolicyCondition
        {
            Id = id,
            TenantId = tenantId,
            DiscountPolicyId = discountPolicyId,
            ConditionGroupNo = conditionGroupNo,
            GroupOperator = groupOperator.Trim().ToUpperInvariant(),
            ConditionType = conditionType.Trim().ToUpperInvariant(),
            ConditionOperator = conditionOperator.Trim().ToUpperInvariant(),
            ConditionValueJson = conditionValueJson,
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        int conditionGroupNo,
        string groupOperator,
        string conditionType,
        string conditionOperator,
        string conditionValueJson,
        int sortOrder,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        ConditionGroupNo = conditionGroupNo;
        GroupOperator = groupOperator.Trim().ToUpperInvariant();
        ConditionType = conditionType.Trim().ToUpperInvariant();
        ConditionOperator = conditionOperator.Trim().ToUpperInvariant();
        ConditionValueJson = conditionValueJson;
        SortOrder = sortOrder;
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
