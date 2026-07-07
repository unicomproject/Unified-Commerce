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

    protected StocktakeLineSerial() { }

    public static StocktakeLineSerial Create(
        Guid id,
        Guid tenantId,
        Guid stocktakeLineId,
        Guid? serialNumberId,
        string scannedSerialNumber,
        string countResult,
        Guid? scannedByTenantUserId,
        DateTimeOffset now)
    {
        return new StocktakeLineSerial
        {
            Id = id,
            TenantId = tenantId,
            StocktakeLineId = stocktakeLineId,
            SerialNumberId = serialNumberId,
            ScannedSerialNumber = scannedSerialNumber.Trim(),
            CountResult = countResult.Trim(),
            ScannedByTenantUserId = scannedByTenantUserId,
            ScannedAt = now,
            CreatedAt = now
        };
    }
}
