namespace E_POS.Application.Modules.Tenant.Inventory.OpeningStock.Dtos;

public sealed record OpeningStockResponse(
    Guid StockMovementId,
    Guid OutletId,
    string MovementType,
    int ItemsCount,
    DateTimeOffset CreatedAt);
