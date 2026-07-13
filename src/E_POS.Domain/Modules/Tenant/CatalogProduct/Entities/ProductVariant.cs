using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductVariant : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public string VariantCode { get; protected set; } = string.Empty;
    public string? Sku { get; protected set; }
    public string VariantName { get; protected set; } = string.Empty;
    public Guid StockUomId { get; protected set; }
    public Guid SalesUomId { get; protected set; }
    public string? OptionCombinationHash { get; protected set; }
    public bool IsDefaultVariant { get; protected set; }
    public bool IsSellable { get; protected set; } = true;
    public bool AllowFractionalQuantity { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductVariant Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        string variantCode,
        string variantName,
        string? sku,
        Guid stockUomId,
        Guid salesUomId,
        bool isDefaultVariant,
        bool isSellable,
        bool allowFractionalQuantity,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductVariant
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            VariantCode = ProductConstants.NormalizeCode(variantCode),
            VariantName = variantName.Trim(),
            Sku = sku?.Trim(),
            StockUomId = stockUomId,
            SalesUomId = salesUomId,
            IsDefaultVariant = isDefaultVariant,
            IsSellable = isSellable,
            AllowFractionalQuantity = allowFractionalQuantity,
            Status = ProductConstants.NormalizeStatus(status),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string variantCode,
        string variantName,
        string? sku,
        Guid stockUomId,
        Guid salesUomId,
        bool isDefaultVariant,
        bool isSellable,
        bool allowFractionalQuantity,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        VariantCode = ProductConstants.NormalizeCode(variantCode);
        VariantName = variantName.Trim();
        Sku = sku?.Trim();
        StockUomId = stockUomId;
        SalesUomId = salesUomId;
        IsDefaultVariant = isDefaultVariant;
        IsSellable = isSellable;
        AllowFractionalQuantity = allowFractionalQuantity;
        Status = ProductConstants.NormalizeStatus(status);
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SetOptionCombinationHash(string hash, DateTimeOffset now)
    {
        OptionCombinationHash = hash;
        UpdatedAt = now;
    }

    public void UpdateStatus(string status, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = ProductConstants.NormalizeStatus(status);
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = ProductConstants.DeletedStatus;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}

