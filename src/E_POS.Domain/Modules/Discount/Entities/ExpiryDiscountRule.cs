using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Discount.Entities;

public class ExpiryDiscountRule : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string RuleCode { get; protected set; } = string.Empty;
}
