using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.PricingTax.Entities;

public class ProductTaxAssignment : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid TaxClassId { get; protected set; }
    public DateTimeOffset? AppliesFrom { get; protected set; }
    public DateTimeOffset? AppliesUntil { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductTaxAssignment Create(
        Guid tenantId,
        Guid productId,
        Guid? productVariantId,
        Guid taxClassId,
        DateTimeOffset? appliesFrom,
        DateTimeOffset? appliesUntil,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductTaxAssignment
        {
            TenantId = tenantId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            TaxClassId = taxClassId,
            AppliesFrom = appliesFrom,
            AppliesUntil = appliesUntil,
            Status = "ACTIVE",
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateAssignment(
        Guid taxClassId,
        DateTimeOffset? appliesFrom,
        DateTimeOffset? appliesUntil,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        TaxClassId = taxClassId;
        AppliesFrom = appliesFrom;
        AppliesUntil = appliesUntil;
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
