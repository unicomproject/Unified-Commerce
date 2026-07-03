namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record BrandResponse(
    Guid Id,
    string BrandCode,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);