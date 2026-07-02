namespace E_POS.Application.Modules.OutletTillDevice.Dtos;

public sealed record TillListResponse(IReadOnlyList<TillSummaryResponse> Items, int PageNumber, int PageSize, int TotalCount);