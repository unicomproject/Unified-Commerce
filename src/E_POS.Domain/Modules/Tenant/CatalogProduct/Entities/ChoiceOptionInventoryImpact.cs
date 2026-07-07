using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ChoiceOptionInventoryImpact : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductChoiceOptionId { get; protected set; }
    public Guid ImpactProductId { get; protected set; }
    public Guid? ImpactVariantId { get; protected set; }
    public Guid ImpactUomId { get; protected set; }
    public string InventoryEffectType { get; protected set; } = string.Empty;
    public decimal Quantity { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ChoiceOptionInventoryImpact Create(
        Guid id,
        Guid tenantId,
        Guid productChoiceOptionId,
        Guid impactProductId,
        Guid? impactVariantId,
        Guid impactUomId,
        string inventoryEffectType,
        decimal quantity,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ChoiceOptionInventoryImpact
        {
            Id = id,
            TenantId = tenantId,
            ProductChoiceOptionId = productChoiceOptionId,
            ImpactProductId = impactProductId,
            ImpactVariantId = impactVariantId,
            ImpactUomId = impactUomId,
            InventoryEffectType = inventoryEffectType.Trim().ToUpperInvariant(),
            Quantity = quantity,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

