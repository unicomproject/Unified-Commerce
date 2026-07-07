using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class InventoryChannelAllocation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid InventoryLocationId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid SalesChannelId { get; protected set; }
    public decimal AllocationLimitQuantity { get; protected set; }
    public decimal SafetyStockQuantity { get; protected set; }
    public bool IsEnabled { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    protected InventoryChannelAllocation() { }

    public static InventoryChannelAllocation Create(
        Guid id,
        Guid tenantId,
        Guid inventoryLocationId,
        Guid productId,
        Guid? productVariantId,
        Guid salesChannelId,
        decimal allocationLimitQuantity,
        decimal safetyStockQuantity,
        bool isEnabled,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new InventoryChannelAllocation
        {
            Id = id,
            TenantId = tenantId,
            InventoryLocationId = inventoryLocationId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            SalesChannelId = salesChannelId,
            AllocationLimitQuantity = allocationLimitQuantity,
            SafetyStockQuantity = safetyStockQuantity,
            IsEnabled = isEnabled,
            Status = status.Trim(),
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        decimal allocationLimitQuantity,
        decimal safetyStockQuantity,
        bool isEnabled,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        AllocationLimitQuantity = allocationLimitQuantity;
        SafetyStockQuantity = safetyStockQuantity;
        IsEnabled = isEnabled;
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
