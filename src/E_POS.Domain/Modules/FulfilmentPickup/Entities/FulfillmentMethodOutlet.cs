using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.FulfilmentPickup.Entities;

public class FulfillmentMethodOutlet : AuditableEntity
{
    public Guid? OutletId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid FulfillmentMethodId { get; protected set; }
}
