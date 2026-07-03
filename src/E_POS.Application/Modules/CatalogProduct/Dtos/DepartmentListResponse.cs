namespace E_POS.Application.Modules.CatalogProduct.Dtos;

public sealed record DepartmentListResponse(IReadOnlyList<DepartmentSummaryResponse> Items, int PageNumber, int PageSize, int TotalCount);