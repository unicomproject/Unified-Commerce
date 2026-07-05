namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record CollectionCreateRequest(
    string CollectionCode, 
    string Name, 
    string? CollectionSlug,
    string? Description,
    string CollectionType,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    int SortOrder,
    string Status);