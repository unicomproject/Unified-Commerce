namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record CollectionResponse(
    Guid Id,
    string CollectionCode,
    string CollectionName,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
