namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record DepartmentListResponse(IReadOnlyList<DepartmentSummaryResponse> Items, int PageNumber, int PageSize, int TotalCount);
