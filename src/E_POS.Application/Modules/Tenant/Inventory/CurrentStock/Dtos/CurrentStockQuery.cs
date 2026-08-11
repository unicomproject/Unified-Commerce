namespace E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Dtos;

public sealed class CurrentStockQuery
{
    public Guid? OutletId { get; set; }
    public string? Search { get; set; }
    public string? StockStatus { get; set; }
    public Guid? CategoryId { get; set; }
    public string? BatchNumber { get; set; }
    public string? ExpiryStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
}
