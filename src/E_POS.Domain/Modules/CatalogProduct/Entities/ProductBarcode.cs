using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.CatalogProduct.Entities;

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
}
