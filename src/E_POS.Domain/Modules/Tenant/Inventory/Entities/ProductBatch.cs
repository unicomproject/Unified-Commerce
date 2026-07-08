using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class ProductBatch : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public string BatchNumber { get; protected set; } = string.Empty;
    public string? SupplierBatchNumber { get; protected set; }
    public DateOnly? ManufacturedAt { get; protected set; }
    public DateOnly? ExpiryDate { get; protected set; }
    public DateTimeOffset? FirstReceivedAt { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    protected ProductBatch() { }

    public static ProductBatch Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        Guid? productVariantId,
        string batchNumber,
        string? supplierBatchNumber,
        DateOnly? manufacturedAt,
        DateOnly? expiryDate,
        DateTimeOffset? firstReceivedAt,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductBatch
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            BatchNumber = batchNumber.Trim(),
            SupplierBatchNumber = supplierBatchNumber?.Trim(),
            ManufacturedAt = manufacturedAt,
            ExpiryDate = expiryDate,
            FirstReceivedAt = firstReceivedAt,
            Status = status.Trim(),
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string? supplierBatchNumber,
        DateOnly? manufacturedAt,
        DateOnly? expiryDate,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        SupplierBatchNumber = supplierBatchNumber?.Trim();
        ManufacturedAt = manufacturedAt;
        ExpiryDate = expiryDate;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void MarkAsReceived(DateTimeOffset receivedAt)
    {
        if (!FirstReceivedAt.HasValue)
        {
            FirstReceivedAt = receivedAt;
        }
    }

    public void UpdateStatus(string status, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = status.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}
