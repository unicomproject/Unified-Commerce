namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record BrandSummaryResponse(
    Guid Id,
    string BrandCode,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);