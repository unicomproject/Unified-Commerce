namespace E_POS.Application.Modules.Tenant.Inventory.Dashboard.Dtos;

public sealed record DashboardMetricsResponse(
    int LowStockCount,
    int OutOfStockCount,
    int NearExpiryCount,
    int ActiveStockCounts);
