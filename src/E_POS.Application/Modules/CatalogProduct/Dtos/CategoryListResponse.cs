namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record CategoryListResponse(IReadOnlyList<CategorySummaryResponse> Items, int PageNumber, int PageSize, int TotalCount);