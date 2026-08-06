namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;

public sealed record TenantAdminOutletListQuery(
    int PageNumber,
    int PageSize,
    string? Search,
    string? OutletType,
    string? Status,
    string? OperationalHealth,
    string? SortBy,
    string? SortDirection);

public sealed record TenantAdminOutletListResponse(
    IReadOnlyList<TenantAdminOutletListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record TenantAdminOutletListItemResponse(
    Guid Id,
    string Name,
    string Code,
    string Type,
    string Status,
    string? ImageUrl,
    TenantAdminOutletManagerPreviewResponse? Manager,
    TenantAdminOutletTillPreviewResponse? Tills,
    TenantAdminOutletHealthPreviewResponse? OperationalHealth,
    TenantAdminOutletLocationPreviewResponse? Location,
    TenantAdminOutletListSectionAccessResponse Access);

public sealed record TenantAdminOutletManagerPreviewResponse(
    Guid TenantUserId,
    string? DisplayName,
    string? AvatarUrl);

public sealed record TenantAdminOutletTillPreviewResponse(
    int TotalCount,
    int ActiveCount,
    int OnlineCount);

public sealed record TenantAdminOutletHealthPreviewResponse(
    string Status,
    int ActiveAlertCount);

public sealed record TenantAdminOutletLocationPreviewResponse(
    string? AddressLine,
    string? City,
    string? DisplayLocation);

public sealed record TenantAdminOutletListSectionAccessResponse(
    bool CanViewTillsAndHealth);

public sealed record TenantAdminOutletStatusUpdateRequest(string Status);

public sealed record TenantAdminOutletLifecycleState(
    bool IsDefaultOutlet,
    bool HasOpenTillSessions,
    bool HasActiveTills,
    bool HasOpenOrders,
    bool HasAllocatedInventory);
