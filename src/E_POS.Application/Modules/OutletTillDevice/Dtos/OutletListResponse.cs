namespace E_POS.Application.Modules.OutletTillDevice.Dtos;

public sealed record OutletListResponse(IReadOnlyList<OutletSummaryResponse> Items, int PageNumber, int PageSize, int TotalCount);