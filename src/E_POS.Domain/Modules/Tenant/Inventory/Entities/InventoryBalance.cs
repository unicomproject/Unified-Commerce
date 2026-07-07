using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class InventoryBalance : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid InventoryLocationId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid? ProductBatchId { get; protected set; }
    public decimal OnHandQuantity { get; protected set; }
    public decimal ReservedQuantity { get; protected set; }
    public decimal DamagedQuantity { get; protected set; }
    public decimal QuarantineQuantity { get; protected set; }
    public decimal AvailableQuantity { get; protected set; }
    public long RowVersion { get; protected set; }

    protected InventoryBalance() { }

    public static InventoryBalance Create(
        Guid id,
        Guid tenantId,
        Guid inventoryLocationId,
        Guid productId,
        Guid? productVariantId,
        Guid? productBatchId,
        DateTimeOffset now)
    {
        return new InventoryBalance
        {
            Id = id,
            TenantId = tenantId,
            InventoryLocationId = inventoryLocationId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            ProductBatchId = productBatchId,
            OnHandQuantity = 0,
            ReservedQuantity = 0,
            DamagedQuantity = 0,
            QuarantineQuantity = 0,
            AvailableQuantity = 0,
            RowVersion = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void AdjustQuantities(
        decimal onHandDelta,
        decimal reservedDelta,
        decimal damagedDelta,
        decimal quarantineDelta,
        DateTimeOffset now)
    {
        OnHandQuantity += onHandDelta;
        ReservedQuantity += reservedDelta;
        DamagedQuantity += damagedDelta;
        QuarantineQuantity += quarantineDelta;
        
        AvailableQuantity = OnHandQuantity - ReservedQuantity - DamagedQuantity - QuarantineQuantity;
        RowVersion++;
        UpdatedAt = now;
    }
}
