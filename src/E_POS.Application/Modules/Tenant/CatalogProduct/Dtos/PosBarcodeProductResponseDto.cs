namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record PosBarcodeProductResponseDto(
    Guid ProductId,
    Guid VariantId,
    string Barcode,
    string BarcodeType,
    string ProductName,
    string VariantName,
    string? Sku,
    decimal QuantityPerScan,
    int Price,
    decimal? AvailableQuantity,
    string StockStatus);
