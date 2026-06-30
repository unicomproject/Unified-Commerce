using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Discount.Entities;

public class DiscountPolicyChannel : AuditableEntity
{
    public string Status { get; protected set; } = string.Empty;
    public Guid DiscountPolicyId { get; protected set; }
    public Guid SalesChannelId { get; protected set; }
}
