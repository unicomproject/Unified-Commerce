using System;
using System.Collections.Generic;

namespace E_POS.Application.Modules.Tenant.Inventory.Dtos.CurrentStock;

public sealed record ProductStockDetailResponse(
    Guid ProductId,
    string ProductName,
    Guid? ProductVariantId,
    string? VariantName,
    string? Sku,
    string? CategoryName,
    string ProductStatus,
    string StockStatus,
    bool BatchTrackingEnabled,
    string? ImageUrl,
    decimal TotalOnHand,
    decimal TotalReserved,
    decimal TotalAvailable,
    decimal TotalReorderLevel,
    IReadOnlyCollection<LocationBalanceDto> LocationBalances
);

public sealed record LocationBalanceDto(
    Guid LocationId,
    string LocationName,
    decimal OnHand,
    decimal Reserved,
    decimal Available,
    decimal ReorderLevel
);
