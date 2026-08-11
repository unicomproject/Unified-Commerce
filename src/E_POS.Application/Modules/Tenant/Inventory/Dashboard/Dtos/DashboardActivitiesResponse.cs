namespace E_POS.Application.Modules.Tenant.Inventory.Dashboard.Dtos;

public sealed record DashboardActivitiesResponse(
    IReadOnlyList<DashboardActivityItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
