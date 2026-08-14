using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductBarcode : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public string Barcode { get; protected set; } = string.Empty;
    public string BarcodeType { get; protected set; } = string.Empty;
    public Guid? UomId { get; protected set; }
    public decimal QuantityPerScan { get; protected set; }
    public bool IsPrimaryBarcode { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static ProductBarcode Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        Guid? productVariantId,
        string barcode,
        string barcodeType,
        Guid? uomId,
        decimal quantityPerScan,
        bool isPrimaryBarcode,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductBarcode
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            Barcode = barcode.Trim(),
            BarcodeType = barcodeType.Trim().ToUpperInvariant(),
            UomId = uomId,
            QuantityPerScan = quantityPerScan,
            IsPrimaryBarcode = isPrimaryBarcode,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
    public void UpdateIdentifier(string barcode, string barcodeType, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Barcode = barcode.Trim();
        BarcodeType = barcodeType.Trim().ToUpperInvariant();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void AssignVariant(Guid? productVariantId, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        ProductVariantId = productVariantId;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void AssignUom(Guid? uomId, decimal quantityPerScan, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        UomId = uomId;
        QuantityPerScan = quantityPerScan;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SetPrimary(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        IsPrimaryBarcode = true;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void ClearPrimary(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        IsPrimaryBarcode = false;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void Deactivate(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = "INACTIVE";
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void Delete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = "DELETED";
        IsPrimaryBarcode = false; // Primary should not be deleted, or at least release the flag
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}

