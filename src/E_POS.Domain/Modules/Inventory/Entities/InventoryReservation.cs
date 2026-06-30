using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class InventoryReservation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string ReservationStatus { get; protected set; } = string.Empty;
    public string ReservationNumber { get; protected set; } = string.Empty;
}
