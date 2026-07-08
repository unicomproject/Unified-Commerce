namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public record ProductResponse(
    Guid Id,
    string ProductCode,
    string Name,
    string? Description,
    string Status,
    string Sku,
    string? Barcode,
    decimal? Price,
    Guid[] CategoryIds,
    Guid[] CollectionIds,
    string[] ImageUrls,
    Guid[] SalesChannelIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

