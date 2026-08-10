namespace E_POS.Application.Modules.Tenant.Inventory.Dtos.StockIn;

public sealed record StockInLineResponse(
    Guid ProductId,
    Guid? VariantId,
    decimal QuantityReceived,
    decimal UnitCost,
    decimal OnHandAfter);
