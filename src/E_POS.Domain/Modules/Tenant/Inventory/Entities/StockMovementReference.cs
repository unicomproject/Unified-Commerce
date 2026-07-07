using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class StockMovementReference : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid StockMovementId { get; protected set; }
    public string ReferenceType { get; protected set; } = string.Empty;
    public Guid ReferenceId { get; protected set; }
    public Guid? ReferenceLineId { get; protected set; }

    protected StockMovementReference() { }

    public static StockMovementReference Create(
        Guid id,
        Guid tenantId,
        Guid stockMovementId,
        string referenceType,
        Guid referenceId,
        Guid? referenceLineId,
        DateTimeOffset now)
    {
        return new StockMovementReference
        {
            Id = id,
            TenantId = tenantId,
            StockMovementId = stockMovementId,
            ReferenceType = referenceType.Trim(),
            ReferenceId = referenceId,
            ReferenceLineId = referenceLineId,
            CreatedAt = now,
            UpdatedAt = now // Required by AuditableEntity base class but not stored
        };
    }
}
