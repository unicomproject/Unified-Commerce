namespace E_POS.Application.Modules.Tenant.Inventory.Dtos.Dashboard;

public sealed record DashboardActivityItemResponse(
    Guid StockMovementId,
    string ActivityType, // "OpeningStockAdded", "StockAdjusted", "SaleOut", "ReturnIn"
    string? ReferenceNumber,
    Guid OutletId,
    string OutletName,
    DateTimeOffset Timestamp,
    decimal ChangeQuantity);
