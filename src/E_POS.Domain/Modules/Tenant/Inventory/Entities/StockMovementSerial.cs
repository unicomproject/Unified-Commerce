using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class StockMovementSerial : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid StockMovementId { get; protected set; }
    public Guid SerialNumberId { get; protected set; }
}
