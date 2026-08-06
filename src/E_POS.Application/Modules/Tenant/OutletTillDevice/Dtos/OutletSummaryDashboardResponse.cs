namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

public sealed record OutletSummaryDashboardResponse(
    int TotalOutlets,
    int ActiveOutlets,
    int WarehouseOutlets,
    int? NeedsAttention);
