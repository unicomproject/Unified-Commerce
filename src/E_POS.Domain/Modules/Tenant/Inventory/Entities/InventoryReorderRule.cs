using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class InventoryReorderRule : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid InventoryLocationId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public string ReorderMethod { get; protected set; } = string.Empty;
    public decimal ReorderPointQuantity { get; protected set; }
    public decimal? ReorderQuantity { get; protected set; }
    public decimal? MinStockQuantity { get; protected set; }
    public decimal? MaxStockQuantity { get; protected set; }
    public decimal SafetyStockQuantity { get; protected set; }
    public int? LeadTimeDays { get; protected set; }
    public Guid? SupplierProductId { get; protected set; }
    public bool IsAutoReorder { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    protected InventoryReorderRule() { }

    public static InventoryReorderRule Create(
        Guid id,
        Guid tenantId,
        Guid inventoryLocationId,
        Guid productId,
        Guid? productVariantId,
        string reorderMethod,
        decimal reorderPointQuantity,
        decimal? reorderQuantity,
        decimal? minStockQuantity,
        decimal? maxStockQuantity,
        decimal safetyStockQuantity,
        int? leadTimeDays,
        Guid? supplierProductId,
        bool isAutoReorder,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new InventoryReorderRule
        {
            Id = id,
            TenantId = tenantId,
            InventoryLocationId = inventoryLocationId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            ReorderMethod = reorderMethod.Trim(),
            ReorderPointQuantity = reorderPointQuantity,
            ReorderQuantity = reorderQuantity,
            MinStockQuantity = minStockQuantity,
            MaxStockQuantity = maxStockQuantity,
            SafetyStockQuantity = safetyStockQuantity,
            LeadTimeDays = leadTimeDays,
            SupplierProductId = supplierProductId,
            IsAutoReorder = isAutoReorder,
            Status = status.Trim(),
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string reorderMethod,
        decimal reorderPointQuantity,
        decimal? reorderQuantity,
        decimal? minStockQuantity,
        decimal? maxStockQuantity,
        decimal safetyStockQuantity,
        int? leadTimeDays,
        Guid? supplierProductId,
        bool isAutoReorder,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        ReorderMethod = reorderMethod.Trim();
        ReorderPointQuantity = reorderPointQuantity;
        ReorderQuantity = reorderQuantity;
        MinStockQuantity = minStockQuantity;
        MaxStockQuantity = maxStockQuantity;
        SafetyStockQuantity = safetyStockQuantity;
        LeadTimeDays = leadTimeDays;
        SupplierProductId = supplierProductId;
        IsAutoReorder = isAutoReorder;
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
