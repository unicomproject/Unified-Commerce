namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record BrandListResponse(
    IReadOnlyList<BrandSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
