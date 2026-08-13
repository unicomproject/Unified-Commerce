using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductUnitSetting : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public string UnitModel { get; protected set; } = ProductUnitModelConstants.SingleUnit;
    public Guid? BaseUomId { get; protected set; }
    public Guid? SellingUomId { get; protected set; }
    public Guid? PurchaseUomId { get; protected set; }
    public Guid? OuterPackUomId { get; protected set; }
    public decimal? ItemsPerPurchaseUnit { get; protected set; }
    public decimal? PurchaseUnitsPerOuterPack { get; protected set; }
    public bool AllowDecimalQuantity { get; protected set; }
    public string Status { get; protected set; } = "ACTIVE";
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductUnitSetting Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        string unitModel,
        Guid? baseUomId,
        Guid? sellingUomId,
        Guid? purchaseUomId,
        Guid? outerPackUomId,
        decimal? itemsPerPurchaseUnit,
        decimal? purchaseUnitsPerOuterPack,
        bool allowDecimalQuantity,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductUnitSetting
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            UnitModel = ProductUnitModelConstants.Normalize(unitModel),
            BaseUomId = baseUomId,
            SellingUomId = sellingUomId,
            PurchaseUomId = purchaseUomId,
            OuterPackUomId = outerPackUomId,
            ItemsPerPurchaseUnit = itemsPerPurchaseUnit,
            PurchaseUnitsPerOuterPack = purchaseUnitsPerOuterPack,
            AllowDecimalQuantity = allowDecimalQuantity,
            Status = "ACTIVE",
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string unitModel,
        Guid? baseUomId,
        Guid? sellingUomId,
        Guid? purchaseUomId,
        Guid? outerPackUomId,
        decimal? itemsPerPurchaseUnit,
        decimal? purchaseUnitsPerOuterPack,
        bool allowDecimalQuantity,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        UnitModel = ProductUnitModelConstants.Normalize(unitModel);
        BaseUomId = baseUomId;
        SellingUomId = sellingUomId;

        if (string.Equals(UnitModel, ProductUnitModelConstants.SingleUnit, StringComparison.OrdinalIgnoreCase))
        {
            PurchaseUomId = baseUomId;
            OuterPackUomId = null;
            ItemsPerPurchaseUnit = null;
            PurchaseUnitsPerOuterPack = null;
        }
        else
        {
            PurchaseUomId = purchaseUomId;
            OuterPackUomId = outerPackUomId;
            ItemsPerPurchaseUnit = itemsPerPurchaseUnit;
            PurchaseUnitsPerOuterPack = outerPackUomId.HasValue ? purchaseUnitsPerOuterPack : null;
        }

        AllowDecimalQuantity = allowDecimalQuantity;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}
