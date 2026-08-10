namespace E_POS.Application.Modules.Tenant.Inventory.Dtos.Dashboard;

public sealed record DashboardMetricsResponse(
    int LowStockCount,
    int OutOfStockCount,
    int NearExpiryCount,
    int ActiveStockCounts);
