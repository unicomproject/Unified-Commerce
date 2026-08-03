namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record PosProductVariantGroupResponseDto(
    string Name,
    IReadOnlyList<string> Options,
    Guid OptionId = default,
    string? OptionCode = null,
    string? InputType = null,
    bool IsRequired = true,
    int SortOrder = 0,
    IReadOnlyList<PosProductOptionValueResponseDto>? Values = null);

public sealed record PosProductOptionValueResponseDto(
    Guid OptionValueId,
    string ValueCode,
    string DisplayName,
    string? ColorHex,
    int SortOrder);

public sealed record PosProductVariantDetailResponseDto(
    Guid VariantId,
    string Sku,
    int Price,
    decimal? StockQty,
    string StockStatus,
    IReadOnlyDictionary<string, string> Attributes,
    string? VariantCode = null,
    string? VariantName = null,
    IReadOnlyList<Guid>? SelectedOptionValueIds = null,
    bool IsDefault = false,
    bool IsSelectable = true,
    string? UnavailableReason = null,
    Guid SalesUomId = default,
    string? SalesUomCode = null,
    bool AllowFractionalQuantity = false,
    decimal? AuthoritativePrice = null,
    string? Currency = null,
    bool IsStockTracked = true,
    string? ImageUrl = null);

public sealed record PosProductDetailResponseDto(
    Guid Id,
    string Name,
    string? Description,
    string? ImageStorageKey,
    string CategoryName,
    int BasePrice,
    bool HasVariants,
    IReadOnlyList<PosProductVariantGroupResponseDto> VariantGroups,
    IReadOnlyList<PosProductVariantDetailResponseDto> Variants,
    string StockStatus = "unknown",
    decimal? AvailableQuantity = null)
{
    public string? ImageUrl => ImageStorageKey;

    public string? ProductCode { get; init; }
    public string Currency { get; init; } = "LKR";
    public bool RequiresConfiguration { get; init; }
}

public sealed record PosProductRecommendationResponseDto(
    Guid RelationshipId,
    Guid ProductId,
    Guid? VariantId,
    string ProductName,
    string? VariantName,
    string? ImageUrl,
    bool HasVariants,
    bool RequiresConfiguration,
    decimal? Price,
    string? Currency,
    decimal? AvailableQuantity,
    string StockStatus,
    bool IsSelectable,
    string? UnavailableReason);
