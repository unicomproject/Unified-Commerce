using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Discount.Entities;

public class DiscountPolicyOutlet : AuditableEntity
{
    public Guid? OutletId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid DiscountPolicyId { get; protected set; }
}
