namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

public sealed record TenantAdminProductDashboardQuery(
    Guid? OutletId,
    DateOnly DateFrom,
    DateOnly DateTo);

public sealed record TenantAdminProductDashboardMetricDto(
    int Value,
    decimal ChangePercent);

public sealed record TenantAdminProductDashboardSummaryDto(
    TenantAdminProductDashboardMetricDto? TotalProducts,
    TenantAdminProductDashboardMetricDto? LowStock,
    TenantAdminProductDashboardMetricDto? OutOfStock,
    TenantAdminProductDashboardMetricDto? ExpiryAlerts,
    TenantAdminProductDashboardMetricDto? StockAdded,
    TenantAdminProductDashboardMetricDto? FastMovingProducts);

public sealed record TenantAdminProductDashboardStockValuePointDto(
    DateOnly Date,
    decimal Value);

public sealed record TenantAdminProductDashboardStockValueDto(
    decimal CurrentValue,
    decimal ChangePercent,
    IReadOnlyList<TenantAdminProductDashboardStockValuePointDto> Trend);

public sealed record TenantAdminProductDashboardMovementItemDto(
    string Type,
    int Count,
    decimal Percentage);

public sealed record TenantAdminProductDashboardStockMovementDto(
    int TotalCount,
    IReadOnlyList<TenantAdminProductDashboardMovementItemDto> Items);

public sealed record TenantAdminProductDashboardResponse(
    DateTimeOffset LastUpdatedAt,
    string CurrencyCode,
    TenantAdminProductDashboardSummaryDto Summary,
    TenantAdminProductDashboardStockValueDto? StockValue,
    TenantAdminProductDashboardStockMovementDto? StockMovement);

public sealed record TenantAdminProductDashboardRawMetric(
    int CurrentValue,
    int PreviousValue);

public sealed record TenantAdminProductDashboardRawStockValuePoint(
    DateOnly Date,
    decimal Value);

public sealed record TenantAdminProductDashboardRawMovement(
    string Type,
    int Count);

public sealed record TenantAdminProductDashboardRawData(
    string CurrencyCode,
    TenantAdminProductDashboardRawMetric TotalProducts,
    TenantAdminProductDashboardRawMetric LowStock,
    TenantAdminProductDashboardRawMetric OutOfStock,
    TenantAdminProductDashboardRawMetric ExpiryAlerts,
    TenantAdminProductDashboardRawMetric StockAdded,
    TenantAdminProductDashboardRawMetric FastMovingProducts,
    decimal CurrentStockValue,
    decimal PreviousStockValue,
    IReadOnlyList<TenantAdminProductDashboardRawStockValuePoint> StockValueTrend,
    IReadOnlyList<TenantAdminProductDashboardRawMovement> StockMovements);
