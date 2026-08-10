namespace E_POS.Application.Modules.Tenant.Inventory.Dtos.CurrentStock;

public sealed record CurrentStockListResponse(
    IReadOnlyList<CurrentStockListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
