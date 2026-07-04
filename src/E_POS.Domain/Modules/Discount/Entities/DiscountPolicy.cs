using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Discount.Entities;

public class DiscountPolicy : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid DiscountTypeId { get; protected set; }
    public string DiscountPolicyCode { get; protected set; } = string.Empty;
    public string DiscountPolicyName { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public string DiscountScope { get; protected set; } = string.Empty;
    public decimal DiscountValue { get; protected set; }
    public string? CurrencyCode { get; protected set; }
    public decimal? MaxDiscountAmount { get; protected set; }
    public decimal? MinOrderAmount { get; protected set; }
    public decimal? MinQuantity { get; protected set; }
    public bool RequiresManagerApproval { get; protected set; }
    public bool IsStackable { get; protected set; }
    public string? StackingGroupCode { get; protected set; }
    public int Priority { get; protected set; }
    public DateTimeOffset? StartsAt { get; protected set; }
    public DateTimeOffset? EndsAt { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
}