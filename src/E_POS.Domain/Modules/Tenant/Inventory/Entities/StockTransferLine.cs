using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

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

    protected StockTransferLine() { }

    public static StockTransferLine Create(
        Guid id,
        Guid tenantId,
        Guid stockTransferId,
        int lineNumber,
        Guid productId,
        Guid? productVariantId,
        Guid? productBatchId,
        decimal requestedQuantity,
        string? lineNote,
        DateTimeOffset now)
    {
        return new StockTransferLine
        {
            Id = id,
            TenantId = tenantId,
            StockTransferId = stockTransferId,
            LineNumber = lineNumber,
            ProductId = productId,
            ProductVariantId = productVariantId,
            ProductBatchId = productBatchId,
            RequestedQuantity = requestedQuantity,
            ShippedQuantity = 0,
            ReceivedQuantity = 0,
            DamagedQuantity = 0,
            MissingQuantity = 0,
            LineStatus = "REQUESTED",
            LineNote = lineNote?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateQuantities(
        decimal shippedDelta,
        decimal receivedDelta,
        decimal damagedDelta,
        decimal missingDelta,
        DateTimeOffset now)
    {
        ShippedQuantity += shippedDelta;
        ReceivedQuantity += receivedDelta;
        DamagedQuantity += damagedDelta;
        MissingQuantity += missingDelta;
        UpdatedAt = now;
    }

    public void UpdateStatus(string status, DateTimeOffset now)
    {
        LineStatus = status.Trim();
        UpdatedAt = now;
    }
}
