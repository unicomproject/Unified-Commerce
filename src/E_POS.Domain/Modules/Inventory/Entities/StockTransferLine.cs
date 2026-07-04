using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Inventory.Entities;

public class StockTransferLine : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid StockTransferId { get; protected set; }
    public int LineNumber { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid? ProductBatchId { get; protected set; }
    public decimal RequestedQuantity { get; protected set; }
    public decimal ShippedQuantity { get; protected set; }
    public decimal ReceivedQuantity { get; protected set; }
    public decimal DamagedQuantity { get; protected set; }
    public decimal MissingQuantity { get; protected set; }
    public string LineStatus { get; protected set; } = string.Empty;
    public string? LineNote { get; protected set; }
}