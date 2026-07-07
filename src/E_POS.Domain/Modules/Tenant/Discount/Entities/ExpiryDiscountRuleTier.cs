using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Discount.Entities;

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
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ExpiryDiscountRuleTier Create(
        Guid id,
        Guid tenantId,
        Guid expiryDiscountRuleId,
        string? tierName,
        int startsDaysBeforeExpiry,
        int endsDaysBeforeExpiry,
        decimal discountPercent,
        int sortOrder,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ExpiryDiscountRuleTier
        {
            Id = id,
            TenantId = tenantId,
            ExpiryDiscountRuleId = expiryDiscountRuleId,
            TierName = tierName?.Trim(),
            StartsDaysBeforeExpiry = startsDaysBeforeExpiry,
            EndsDaysBeforeExpiry = endsDaysBeforeExpiry,
            DiscountPercent = discountPercent,
            SortOrder = sortOrder,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string? tierName,
        int startsDaysBeforeExpiry,
        int endsDaysBeforeExpiry,
        decimal discountPercent,
        int sortOrder,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        TierName = tierName?.Trim();
        StartsDaysBeforeExpiry = startsDaysBeforeExpiry;
        EndsDaysBeforeExpiry = endsDaysBeforeExpiry;
        DiscountPercent = discountPercent;
        SortOrder = sortOrder;
        Status = status.Trim().ToUpperInvariant();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = "DELETED";
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}
