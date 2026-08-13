using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Services;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class ProductInventorySetting : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid InventoryUomId { get; protected set; }
    public bool IsStockTracked { get; protected set; }
    public bool AllowNegativeStock { get; protected set; }
    public bool RequiresBatchTracking { get; protected set; }
    public bool RequiresExpiryTracking { get; protected set; }
    public bool RequiresSerialTracking { get; protected set; }
    public string CostingMethod { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    protected ProductInventorySetting() { }

    public static ProductInventorySetting Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        Guid? productVariantId,
        Guid inventoryUomId,
        bool isStockTracked,
        bool allowNegativeStock,
        bool requiresBatchTracking,
        bool requiresExpiryTracking,
        bool requiresSerialTracking,
        string costingMethod,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        var (normalizedStock, normalizedBatch, normalizedExpiry, normalizedSerial) =
            ProductTrackingRules.NormalizeProfile(isStockTracked, requiresBatchTracking, requiresExpiryTracking, requiresSerialTracking);

        return new ProductInventorySetting
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            InventoryUomId = inventoryUomId,
            IsStockTracked = normalizedStock,
            AllowNegativeStock = allowNegativeStock,
            RequiresBatchTracking = normalizedBatch,
            RequiresExpiryTracking = normalizedExpiry,
            RequiresSerialTracking = normalizedSerial,
            CostingMethod = costingMethod.Trim(),
            Status = status.Trim(),
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        Guid inventoryUomId,
        bool isStockTracked,
        bool allowNegativeStock,
        bool requiresBatchTracking,
        bool requiresExpiryTracking,
        bool requiresSerialTracking,
        string costingMethod,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        var (normalizedStock, normalizedBatch, normalizedExpiry, normalizedSerial) =
            ProductTrackingRules.NormalizeProfile(isStockTracked, requiresBatchTracking, requiresExpiryTracking, requiresSerialTracking);

        InventoryUomId = inventoryUomId;
        IsStockTracked = normalizedStock;
        AllowNegativeStock = allowNegativeStock;
        RequiresBatchTracking = normalizedBatch;
        RequiresExpiryTracking = normalizedExpiry;
        RequiresSerialTracking = normalizedSerial;
        CostingMethod = costingMethod.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void UpdateStatus(string status, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = status.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}
