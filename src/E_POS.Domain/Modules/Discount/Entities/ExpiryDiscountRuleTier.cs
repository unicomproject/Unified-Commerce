using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Discount.Entities;

public class ExpiryDiscountRuleTier : AuditableEntity
{
    public int DaysBeforeExpiry { get; protected set; }
    public decimal DiscountValue { get; protected set; }
    public Guid ExpiryDiscountRuleId { get; protected set; }
}
