namespace E_POS.Application.Modules.Tenant.Inventory.Dtos.Dashboard;

public sealed record DashboardActivitiesResponse(
    IReadOnlyList<DashboardActivityItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
