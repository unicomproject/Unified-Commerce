using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Discount.Entities;

public class DiscountPolicyChannel : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid DiscountPolicyId { get; protected set; }
    public Guid SalesChannelId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static DiscountPolicyChannel Create(
        Guid id,
        Guid tenantId,
        Guid discountPolicyId,
        Guid salesChannelId,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new DiscountPolicyChannel
        {
            Id = id,
            TenantId = tenantId,
            DiscountPolicyId = discountPolicyId,
            SalesChannelId = salesChannelId,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateStatus(string status, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
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
