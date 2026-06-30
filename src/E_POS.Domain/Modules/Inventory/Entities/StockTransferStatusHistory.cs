using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class StockTransferStatusHistory : AuditableEntity
{
    public int SequenceNumber { get; protected set; }
    public Guid StockTransferId { get; protected set; }
}
