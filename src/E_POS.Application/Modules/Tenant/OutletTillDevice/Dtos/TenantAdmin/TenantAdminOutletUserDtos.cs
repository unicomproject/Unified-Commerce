namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;

public sealed record TenantAdminOutletAssignedUserResponse(
    Guid UserId,
    string DisplayName,
    string RoleName,
    string? AssignedTillOrDepartment,
    string? PhoneNumber,
    string? Email,
    string? OutletAccess,
    string Status,
    DateTimeOffset? LastActivity);

public sealed record TenantAdminOutletUsersSummaryResponse(
    int TotalAssignedUsers,
    int ActiveUsers,
    int PendingInvites,
    int Managers);

public sealed record TenantAdminOutletUsersResponse(
    TenantAdminOutletUsersSummaryResponse Summary,
    IReadOnlyList<TenantAdminOutletAssignedUserResponse> Items);
