using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.FulfilmentPickup.Entities;

public class PickupSlot : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid FulfillmentMethodOutletId { get; protected set; }
    public string SlotCode { get; protected set; } = string.Empty;
    public DateOnly SlotDate { get; protected set; }
    public TimeOnly WindowStart { get; protected set; }
    public TimeOnly WindowEnd { get; protected set; }
    public int Capacity { get; protected set; }
    public int ReservedCount { get; protected set; }
    public string SlotStatus { get; protected set; } = string.Empty;
    public long RowVersion { get; protected set; }
}
