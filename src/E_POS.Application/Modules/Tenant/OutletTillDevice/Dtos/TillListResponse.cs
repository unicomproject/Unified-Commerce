namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record TillListResponse(IReadOnlyList<TillSummaryResponse> Items, int PageNumber, int PageSize, int TotalCount);
