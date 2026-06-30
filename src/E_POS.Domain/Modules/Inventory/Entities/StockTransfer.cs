using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class StockTransfer : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid DestinationInventoryLocationId { get; protected set; }
    public Guid SourceInventoryLocationId { get; protected set; }
    public string TransferNumber { get; protected set; } = string.Empty;
}
