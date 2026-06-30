using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Discount.Entities;

public class DiscountPolicy : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string DiscountCode { get; protected set; } = string.Empty;
    public Guid DiscountTypeId { get; protected set; }
    public decimal DiscountValue { get; protected set; }
}
