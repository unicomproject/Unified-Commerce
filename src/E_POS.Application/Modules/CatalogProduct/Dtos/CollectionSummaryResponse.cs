namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record CollectionSummaryResponse(
    Guid Id,
    string CollectionCode,
    string CollectionName,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);