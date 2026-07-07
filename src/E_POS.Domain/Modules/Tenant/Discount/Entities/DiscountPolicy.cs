using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Discount.Entities;

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
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static DiscountPolicy Create(
        Guid id,
        Guid tenantId,
        Guid discountTypeId,
        string discountPolicyCode,
        string discountPolicyName,
        string? description,
        string discountScope,
        decimal discountValue,
        string? currencyCode,
        decimal? maxDiscountAmount,
        decimal? minOrderAmount,
        decimal? minQuantity,
        bool requiresManagerApproval,
        bool isStackable,
        string? stackingGroupCode,
        int priority,
        DateTimeOffset? startsAt,
        DateTimeOffset? endsAt,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new DiscountPolicy
        {
            Id = id,
            TenantId = tenantId,
            DiscountTypeId = discountTypeId,
            DiscountPolicyCode = discountPolicyCode.Trim().ToUpperInvariant(),
            DiscountPolicyName = discountPolicyName.Trim(),
            Description = description?.Trim(),
            DiscountScope = discountScope.Trim().ToUpperInvariant(),
            DiscountValue = discountValue,
            CurrencyCode = currencyCode?.Trim().ToUpperInvariant(),
            MaxDiscountAmount = maxDiscountAmount,
            MinOrderAmount = minOrderAmount,
            MinQuantity = minQuantity,
            RequiresManagerApproval = requiresManagerApproval,
            IsStackable = isStackable,
            StackingGroupCode = stackingGroupCode?.Trim().ToUpperInvariant(),
            Priority = priority,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        Guid discountTypeId,
        string discountPolicyCode,
        string discountPolicyName,
        string? description,
        string discountScope,
        decimal discountValue,
        string? currencyCode,
        decimal? maxDiscountAmount,
        decimal? minOrderAmount,
        decimal? minQuantity,
        bool requiresManagerApproval,
        bool isStackable,
        string? stackingGroupCode,
        int priority,
        DateTimeOffset? startsAt,
        DateTimeOffset? endsAt,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        DiscountTypeId = discountTypeId;
        DiscountPolicyCode = discountPolicyCode.Trim().ToUpperInvariant();
        DiscountPolicyName = discountPolicyName.Trim();
        Description = description?.Trim();
        DiscountScope = discountScope.Trim().ToUpperInvariant();
        DiscountValue = discountValue;
        CurrencyCode = currencyCode?.Trim().ToUpperInvariant();
        MaxDiscountAmount = maxDiscountAmount;
        MinOrderAmount = minOrderAmount;
        MinQuantity = minQuantity;
        RequiresManagerApproval = requiresManagerApproval;
        IsStackable = isStackable;
        StackingGroupCode = stackingGroupCode?.Trim().ToUpperInvariant();
        Priority = priority;
        StartsAt = startsAt;
        EndsAt = endsAt;
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
