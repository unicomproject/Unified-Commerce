using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class InventoryReservationLine : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid InventoryReservationId { get; protected set; }
    public int LineNumber { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public decimal RequestedQuantity { get; protected set; }
    public decimal ReservedQuantity { get; protected set; }
    public decimal ReleasedQuantity { get; protected set; }
    public decimal FulfilledQuantity { get; protected set; }
    public string LineStatus { get; protected set; } = string.Empty;
}
