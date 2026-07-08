namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public record ProductSummaryResponse(
    Guid Id,
    string ProductCode,
    string Name,
    string Status,
    string Sku,
    string? Barcode,
    decimal? Price,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

