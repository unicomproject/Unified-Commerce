namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;

public sealed record TenantAdminOutletRevenuePointResponse(
    string Label,
    decimal Amount);

public sealed record TenantAdminOutletPaymentMethodShareResponse(
    string Method,
    decimal Amount,
    decimal Percent);

public sealed record TenantAdminOutletRevenueSummaryBreakdownResponse(
    decimal GrossRevenue,
    decimal Discounts,
    decimal Returns,
    decimal NetRevenue,
    decimal TaxCollected);

public sealed record TenantAdminOutletRevenueSummaryResponse(
    decimal TotalRevenue,
    decimal AverageOrderValue,
    int TotalOrders,
    decimal Refunds,
    decimal? RevenueChangePercent,
    decimal? AverageOrderValueChangePercent,
    decimal? OrdersChangePercent,
    decimal? RefundsChangePercent,
    IReadOnlyList<TenantAdminOutletRevenuePointResponse> RevenueOverTime,
    IReadOnlyList<TenantAdminOutletPaymentMethodShareResponse> RevenueByPaymentMethod,
    TenantAdminOutletRevenueSummaryBreakdownResponse RevenueSummary);
