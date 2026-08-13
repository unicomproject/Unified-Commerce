namespace E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Dtos;

public sealed record CurrentStockSummaryResponse(
    int TotalItemsInStock,
    int TotalItemsLowStock,
    int TotalItemsOutOfStock,
    decimal TotalInventoryValue);
