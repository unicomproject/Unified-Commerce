namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record PosDeviceListResponse(
    IReadOnlyList<PosDeviceSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
