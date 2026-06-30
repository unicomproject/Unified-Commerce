using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class StockMovementReference : AuditableEntity
{
    public string ReferenceType { get; protected set; } = string.Empty;
    public Guid ReferenceId { get; protected set; }
    public Guid StockMovementId { get; protected set; }
}
