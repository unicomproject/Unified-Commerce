namespace E_POS.Application.Modules.Tenant.Inventory.StockIn.Dtos;

public sealed class StockInRequest
{
    public Guid OutletId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<StockInLineRequest> Items { get; set; } = [];
    public string? IdempotencyKey { get; set; }
}
