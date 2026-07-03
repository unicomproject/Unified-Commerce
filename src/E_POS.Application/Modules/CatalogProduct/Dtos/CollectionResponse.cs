namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record CollectionResponse(
    Guid Id,
    string CollectionCode,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);