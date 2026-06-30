using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Discount.Entities;

public class DiscountPolicyTarget : AuditableEntity
{
    public Guid DiscountPolicyId { get; protected set; }
    public Guid TargetId { get; protected set; }
    public string TargetType { get; protected set; } = string.Empty;
}
