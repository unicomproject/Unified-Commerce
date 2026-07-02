namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformRoleListItemDto(
    Guid Id,
    string RoleCode,
    string Name,
    string? Description,
    string Status,
    int PermissionCount,
    int UserCount,
    bool IsSystem,
    bool IsProtected,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PlatformRoleListResponse(
    IReadOnlyList<PlatformRoleListItemDto> Roles);

public sealed record PlatformRoleDetailResponse(
    Guid Id,
    string RoleCode,
    string Name,
    string? Description,
    string Status,
    int PermissionCount,
    int UserCount,
    bool IsSystem,
    bool IsProtected,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreatePlatformRoleRequest
{
    public string? RoleCode { get; init; }

    public string? Code { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? Status { get; init; }
}

public sealed record UpdatePlatformRoleRequest
{
    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? Status { get; init; }
}

public sealed record PlatformRolePermissionsResponse(
    Guid RoleId,
    string RoleCode,
    string RoleName,
    IReadOnlyList<string> AssignedPermissionCodes,
    IReadOnlyList<PlatformPermissionDto> AvailablePermissions,
    DateTimeOffset UpdatedAt);

public sealed record UpdatePlatformRolePermissionsRequest
{
    public IReadOnlyList<string>? PermissionCodes { get; init; }
}
