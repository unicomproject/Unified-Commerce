namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record CollectionProductResponseDto(
    Guid ProductId,
    string ProductName,
    string? Sku,
    string Status,
    int SortOrder);
