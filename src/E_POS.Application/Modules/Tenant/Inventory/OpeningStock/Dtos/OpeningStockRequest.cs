namespace E_POS.Application.Modules.Tenant.Inventory.OpeningStock.Dtos;

public sealed class OpeningStockRequest
{
    public Guid OutletId { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<OpeningStockLineRequest> Items { get; set; } = [];
    public string? IdempotencyKey { get; set; }
}
