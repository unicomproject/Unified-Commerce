using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

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
}