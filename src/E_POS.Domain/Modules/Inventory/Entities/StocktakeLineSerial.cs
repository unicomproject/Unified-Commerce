using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class StocktakeLineSerial : AuditableEntity
{
    public Guid SerialNumberId { get; protected set; }
    public Guid StocktakeLineId { get; protected set; }
}
