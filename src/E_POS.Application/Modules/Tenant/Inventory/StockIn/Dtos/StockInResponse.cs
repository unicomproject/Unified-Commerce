namespace E_POS.Application.Modules.Tenant.Inventory.StockIn.Dtos;

public sealed record StockInResponse(
    Guid StockMovementId,
    Guid OutletId,
    string MovementType,
    string? ReferenceNumber,
    IReadOnlyList<StockInLineResponse> Items,
    DateTimeOffset CreatedAt);
