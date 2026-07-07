using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class StocktakeLineSerial : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid StocktakeLineId { get; protected set; }
    public Guid? SerialNumberId { get; protected set; }
    public string ScannedSerialNumber { get; protected set; } = string.Empty;
    public string CountResult { get; protected set; } = string.Empty;
    public Guid? ScannedByTenantUserId { get; protected set; }
    public DateTimeOffset ScannedAt { get; protected set; }
}
