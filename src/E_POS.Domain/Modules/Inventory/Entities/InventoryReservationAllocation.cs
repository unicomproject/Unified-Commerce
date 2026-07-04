using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class InventoryReservationAllocation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid InventoryReservationLineId { get; protected set; }
    public Guid InventoryBalanceId { get; protected set; }
    public Guid? SerialNumberId { get; protected set; }
    public decimal AllocatedQuantity { get; protected set; }
    public decimal ReleasedQuantity { get; protected set; }
    public decimal FulfilledQuantity { get; protected set; }
    public string AllocationStatus { get; protected set; } = string.Empty;
    public DateTimeOffset AllocatedAt { get; protected set; }
    public DateTimeOffset? ReleasedAt { get; protected set; }
}