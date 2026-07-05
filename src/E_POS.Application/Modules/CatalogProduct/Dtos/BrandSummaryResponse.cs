namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record BrandSummaryResponse(
    Guid Id,
    string BrandCode,
    string BrandName,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);