using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class StockMovementSerial : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid StockMovementId { get; protected set; }
    public Guid SerialNumberId { get; protected set; }

    protected StockMovementSerial() { }

    public static StockMovementSerial Create(
        Guid id,
        Guid tenantId,
        Guid stockMovementId,
        Guid serialNumberId,
        DateTimeOffset now)
    {
        return new StockMovementSerial
        {
            Id = id,
            TenantId = tenantId,
            StockMovementId = stockMovementId,
            SerialNumberId = serialNumberId,
            CreatedAt = now,
            UpdatedAt = now // Required by AuditableEntity base class but not stored
        };
    }
}
