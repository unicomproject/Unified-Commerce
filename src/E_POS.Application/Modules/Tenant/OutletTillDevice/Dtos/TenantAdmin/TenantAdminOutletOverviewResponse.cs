namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;

public sealed record TenantAdminOutletOverviewResponse(
    OutletOverviewInfoResponse Outlet,
    OutletOverviewManagerResponse? Manager,
    OutletOverviewTillSummaryResponse? Tills,
    OutletOverviewSalesSummaryResponse? Sales,
    OutletOverviewInventorySummaryResponse? Inventory,
    OutletOverviewOrderSummaryResponse? Orders,
    OutletOverviewHealthResponse Health,
    IReadOnlyList<OutletOverviewAlertResponse>? Alerts,
    int TotalActiveAlertCount,
    OutletOverviewSectionAccessResponse Access);

public sealed record OutletOverviewInfoResponse(
    Guid Id,
    string Name,
    string Code,
    string Type,
    string Status,
    string? ImageUrl,
    string? AddressLine1,
    string? City,
    Guid? MediaAssetId = null);

public sealed record OutletOverviewManagerResponse(
    Guid? TenantUserId,
    string? Name,
    string? Email = null,
    string? Phone = null,
    string? AvatarUrl = null);

public sealed record OutletOverviewTillSummaryResponse(
    int TotalCount,
    int ActiveCount,
    int OnlineCount,
    int AttentionCount);

public sealed record OutletOverviewSalesSummaryResponse(
    decimal TodayNetSales,
    decimal? YesterdayComparisonPercentage,
    string CurrencyCode);

public sealed record OutletOverviewInventorySummaryResponse(
    decimal StockValue,
    string CurrencyCode);

public sealed record OutletOverviewOrderSummaryResponse(
    int OpenOrderCount);

public sealed record OutletOverviewHealthResponse(
    string Status,
    DateTimeOffset? LastActivityAt,
    DateTimeOffset? LastSyncAt);

public sealed record OutletOverviewAlertResponse(
    string AlertId,
    string Title,
    string Severity,
    string Description,
    DateTimeOffset OccurredAt);

public sealed record OutletOverviewSectionAccessResponse(
    bool CanViewTills,
    bool CanViewSales,
    bool CanViewInventory,
    bool CanViewOrders,
    bool CanViewAlerts);

public sealed record TenantAdminOutletManagerUpdateRequest(
    Guid TenantUserId);

public sealed record TenantAdminOutletImageUpdateRequest(
    Guid MediaAssetId);
