using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Discount.Entities;

public class ExpiryDiscountApplication : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ExpiryDiscountRuleId { get; protected set; }
    public Guid ExpiryDiscountRuleTierId { get; protected set; }
    public Guid ProductBatchId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public decimal DiscountPercent { get; protected set; }
    public string ApplicationSource { get; protected set; } = string.Empty;
    public string ApplicationStatus { get; protected set; } = string.Empty;
    public DateTimeOffset AppliedFrom { get; protected set; }
    public DateTimeOffset? AppliedUntil { get; protected set; }
    public Guid? ApprovedByTenantUserId { get; protected set; }
    public DateTimeOffset? ApprovedAt { get; protected set; }
    public string? ApprovalNote { get; protected set; }
}