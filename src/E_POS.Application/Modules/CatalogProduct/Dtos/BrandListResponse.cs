namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record BrandListResponse(
    IReadOnlyList<BrandSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);