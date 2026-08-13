namespace E_POS.Application.Modules.Tenant.Inventory.StockIn.Dtos;

public sealed record StockInLineResponse(
    Guid ProductId,
    Guid? VariantId,
    decimal QuantityReceived,
    decimal UnitCost,
    decimal OnHandAfter);
