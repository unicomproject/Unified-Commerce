using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductUnitConversion : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid UomId { get; protected set; }
    public string UnitLevel { get; protected set; } = "BASE";
    public decimal ConversionToBaseFactor { get; protected set; }
    public bool IsBaseUnit { get; protected set; }
    public bool IsSellingUnit { get; protected set; }
    public bool IsPurchaseUnit { get; protected set; }
    public bool IsOuterPackUnit { get; protected set; }
    public string Status { get; protected set; } = "ACTIVE";
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductUnitConversion Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        Guid uomId,
        string unitLevel,
        decimal conversionToBaseFactor,
        bool isBaseUnit,
        bool isSellingUnit,
        bool isPurchaseUnit,
        bool isOuterPackUnit,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductUnitConversion
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            UomId = uomId,
            UnitLevel = unitLevel.Trim().ToUpperInvariant(),
            ConversionToBaseFactor = conversionToBaseFactor,
            IsBaseUnit = isBaseUnit,
            IsSellingUnit = isSellingUnit,
            IsPurchaseUnit = isPurchaseUnit,
            IsOuterPackUnit = isOuterPackUnit,
            Status = "ACTIVE",
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
