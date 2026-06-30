using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class InventoryReservationAllocation : AuditableEntity
{
    public decimal AllocatedQuantity { get; protected set; }
    public Guid InventoryBalanceId { get; protected set; }
    public Guid InventoryReservationLineId { get; protected set; }
}
