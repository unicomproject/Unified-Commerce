namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record BrandListResponse(
    IReadOnlyList<BrandSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
