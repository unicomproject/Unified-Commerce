namespace E_POS.Application.Modules.Tenant.Inventory.Dtos.CurrentStock;

public sealed record CurrentStockSummaryResponse(
    int TotalItemsInStock,
    int TotalItemsLowStock,
    int TotalItemsOutOfStock,
    decimal TotalInventoryValue);
