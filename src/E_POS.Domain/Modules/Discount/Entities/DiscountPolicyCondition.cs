using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Discount.Entities;

public class DiscountPolicyCondition : AuditableEntity
{
    public int ConditionSequence { get; protected set; }
    public Guid DiscountPolicyId { get; protected set; }
}
