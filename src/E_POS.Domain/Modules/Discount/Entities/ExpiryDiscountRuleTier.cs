using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Discount.Entities;

public class ExpiryDiscountRuleTier : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ExpiryDiscountRuleId { get; protected set; }
    public string? TierName { get; protected set; }
    public int StartsDaysBeforeExpiry { get; protected set; }
    public int EndsDaysBeforeExpiry { get; protected set; }
    public decimal DiscountPercent { get; protected set; }
    public int SortOrder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}