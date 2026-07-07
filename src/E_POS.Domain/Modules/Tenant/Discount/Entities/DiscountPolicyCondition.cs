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
}
