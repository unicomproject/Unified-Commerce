namespace E_POS.Application.Modules.Tenant.Inventory.Dashboard.Dtos;

public sealed record DashboardActivityItemResponse(
    Guid StockMovementId,
    string ActivityType, // "OpeningStockAdded", "StockAdjusted", "SaleOut", "ReturnIn"
    string? ReferenceNumber,
    Guid OutletId,
    string OutletName,
    DateTimeOffset Timestamp,
    decimal ChangeQuantity);
