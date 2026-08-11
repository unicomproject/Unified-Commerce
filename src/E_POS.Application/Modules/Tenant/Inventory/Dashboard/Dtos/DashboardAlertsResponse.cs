namespace E_POS.Application.Modules.Tenant.Inventory.Dashboard.Dtos;

public sealed record DashboardAlertsResponse(
    IReadOnlyList<DashboardAlertItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
