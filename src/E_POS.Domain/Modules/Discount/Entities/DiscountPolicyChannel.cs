using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Discount.Entities;

public class DiscountPolicyChannel : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid DiscountPolicyId { get; protected set; }
    public Guid SalesChannelId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}