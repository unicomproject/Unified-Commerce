using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Discount.Entities;

public class DiscountPolicyTarget : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid DiscountPolicyId { get; protected set; }
    public string TargetType { get; protected set; } = string.Empty;
    public string TargetMode { get; protected set; } = string.Empty;
    public Guid? ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid? CategoryId { get; protected set; }
    public Guid? BrandId { get; protected set; }
    public Guid? CollectionId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static DiscountPolicyTarget Create(
        Guid id,
        Guid tenantId,
        Guid discountPolicyId,
        string targetType,
        string targetMode,
        Guid? productId,
        Guid? productVariantId,
        Guid? categoryId,
        Guid? brandId,
        Guid? collectionId,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new DiscountPolicyTarget
        {
            Id = id,
            TenantId = tenantId,
            DiscountPolicyId = discountPolicyId,
            TargetType = targetType.Trim().ToUpperInvariant(),
            TargetMode = targetMode.Trim().ToUpperInvariant(),
            ProductId = productId,
            ProductVariantId = productVariantId,
            CategoryId = categoryId,
            BrandId = brandId,
            CollectionId = collectionId,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string targetType,
        string targetMode,
        Guid? productId,
        Guid? productVariantId,
        Guid? categoryId,
        Guid? brandId,
        Guid? collectionId,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        TargetType = targetType.Trim().ToUpperInvariant();
        TargetMode = targetMode.Trim().ToUpperInvariant();
        ProductId = productId;
        ProductVariantId = productVariantId;
        CategoryId = categoryId;
        BrandId = brandId;
        CollectionId = collectionId;
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
