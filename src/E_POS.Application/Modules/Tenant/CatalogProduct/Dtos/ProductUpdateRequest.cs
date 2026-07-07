namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public record ProductUpdateRequest(
    string Name,
    string? Description,
    string? ShortDescription,
    string? LongDescription,
    string Status,
    string ProductCode,
    string? ProductType,
    string? ProductStructure,
    Guid? BusinessTypeId,
    Guid? BrandId,
    Guid? ReturnPolicyId,
    bool? IsSellable,
    bool? IsTaxable,
    string Sku,
    Guid? StockUomId,
    Guid? SalesUomId,
    string? Barcode,
    decimal? Price,
    Guid[]? CategoryIds,
    Guid[]? CollectionIds,
    string[]? ImageUrls,
    Guid[]? SalesChannelIds);

