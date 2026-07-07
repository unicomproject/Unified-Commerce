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
}
