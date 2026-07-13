namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record PosProductVariantGroupResponseDto(
    string Name,
    IReadOnlyList<string> Options);

public sealed record PosProductVariantDetailResponseDto(
    Guid VariantId,
    string Sku,
    int Price,
    decimal? StockQty,
    string StockStatus,
    IReadOnlyDictionary<string, string> Attributes);

public sealed record PosProductDetailResponseDto(
    Guid Id,
    string Name,
    string? Description,
    string? ImageStorageKey,
    string CategoryName,
    int BasePrice,
    bool HasVariants,
    IReadOnlyList<PosProductVariantGroupResponseDto> VariantGroups,
    IReadOnlyList<PosProductVariantDetailResponseDto> Variants)
{
    public string? ImageUrl => ImageStorageKey;
}
