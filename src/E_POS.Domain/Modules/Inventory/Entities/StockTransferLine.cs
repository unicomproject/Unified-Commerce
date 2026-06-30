using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class StockTransferLine : AuditableEntity
{
    public string LineNumber { get; protected set; } = string.Empty;
    public Guid ProductId { get; protected set; }
    public decimal RequestedQuantity { get; protected set; }
    public Guid StockTransferId { get; protected set; }
}
