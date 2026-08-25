namespace E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Dtos;

public sealed class StockTransferRequest
{
    public Guid SourceOutletId { get; set; }
    public Guid DestinationOutletId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<StockTransferLineRequest> Items { get; set; } = [];
    public string? IdempotencyKey { get; set; }
}

public sealed class StockTransferLineRequest
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal TransferQuantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}

public sealed class StockTransferResponse
{
    public Guid StockMovementId { get; set; }
    public Guid SourceOutletId { get; set; }
    public Guid DestinationOutletId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; set; }
}
