namespace E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Dtos;

public sealed class StockAdjustmentRequest
{
    public Guid OutletId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<StockAdjustmentLineRequest> Items { get; set; } = [];
    public string? IdempotencyKey { get; set; }
}

public sealed class StockAdjustmentLineRequest
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal AdjustedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}

public sealed class StockAdjustmentResponse
{
    public Guid StockMovementId { get; set; }
    public Guid OutletId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
