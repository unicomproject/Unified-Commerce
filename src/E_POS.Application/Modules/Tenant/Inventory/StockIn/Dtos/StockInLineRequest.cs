namespace E_POS.Application.Modules.Tenant.Inventory.StockIn.Dtos;

public sealed class StockInLineRequest
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}
