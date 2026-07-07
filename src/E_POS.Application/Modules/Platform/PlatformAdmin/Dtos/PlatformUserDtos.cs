namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformUserListItemDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string Status,
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<string> RoleNames,
    int PermissionCount,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PlatformUserListResponse(
    IReadOnlyList<PlatformUserListItemDto> Users);

public sealed record PlatformUserDetailResponse(
    Guid Id,
    string Email,
    string? DisplayName,
    string Status,
    bool InvitePending,
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<string> RoleNames,
    int PermissionCount,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreatePlatformUserRequest
{
    public string? Email { get; init; }

    public string? Status { get; init; }

    public IReadOnlyList<Guid>? RoleIds { get; init; }

    public IReadOnlyList<string>? RoleCodes { get; init; }
}

public sealed record UpdatePlatformUserRequest
{
    public string? Status { get; init; }
}

public sealed record AssignPlatformUserRolesRequest
{
    public IReadOnlyList<Guid>? RoleIds { get; init; }

    public IReadOnlyList<string>? RoleCodes { get; init; }
}

public sealed record ResolvedPlatformRole(
    Guid Id,
    string RoleCode,
    string Name);

