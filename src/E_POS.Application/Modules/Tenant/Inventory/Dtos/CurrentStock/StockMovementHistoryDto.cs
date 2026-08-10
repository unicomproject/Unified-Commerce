using System;
using E_POS.Application.Common.Models;

namespace E_POS.Application.Modules.Tenant.Inventory.Dtos.CurrentStock;

public sealed record StockMovementHistoryDto(
    Guid MovementId,
    string MovementType,
    string? Reference,
    string LocationName,
    DateTimeOffset Date,
    decimal Change
);

public sealed class StockMovementHistoryQuery
{
    public Guid ProductVariantId { get; set; }
    public Guid? OutletId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public sealed record StockMovementHistoryListResponse(
    IReadOnlyList<StockMovementHistoryDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
