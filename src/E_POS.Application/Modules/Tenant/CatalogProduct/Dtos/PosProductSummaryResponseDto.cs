namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record PosProductSummaryResponseDto(
    Guid Id,
    Guid? VariantId,
    string Name,
    string? Description,
    string? ImageStorageKey,
    Guid? CategoryId,
    string CategoryName,
    int BasePrice,
    bool HasVariants,
    string StockStatus,
    decimal? AvailableQuantity);
