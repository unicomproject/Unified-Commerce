namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record CollectionUpdateRequest(
    string CollectionCode, 
    string Name, 
    string? CollectionSlug,
    string? Description,
    string CollectionType,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    int SortOrder,
    string Status);