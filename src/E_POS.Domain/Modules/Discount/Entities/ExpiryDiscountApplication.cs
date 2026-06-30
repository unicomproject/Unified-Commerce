using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Discount.Entities;

public class ExpiryDiscountApplication : AuditableEntity
{
    public Guid ExpiryDiscountRuleId { get; protected set; }
    public Guid? ProductBatchId { get; protected set; }
}
