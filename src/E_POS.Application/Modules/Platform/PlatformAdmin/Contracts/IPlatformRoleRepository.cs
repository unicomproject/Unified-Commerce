using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformRoleRepository
{
    Task<PlatformRoleListResponse> GetRolesAsync(CancellationToken cancellationToken);

    Task<PlatformRoleDetailResponse?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken);

    Task<PlatformRole?> GetRoleEntityByIdAsync(Guid roleId, CancellationToken cancellationToken);

    Task<bool> RoleCodeExistsAsync(string roleCode, CancellationToken cancellationToken);

    Task AddRoleAsync(PlatformRole role, CancellationToken cancellationToken);

    Task UpdateRoleAsync(PlatformRole role, CancellationToken cancellationToken);

    Task<PlatformRolePermissionsResponse?> GetRolePermissionsAsync(
        IReadOnlyList<PlatformPermissionDto> availablePermissions,
        Guid roleId,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, Guid>> GetActiveBusinessPermissionIdMapAsync(
        CancellationToken cancellationToken);

    Task ReplaceRolePermissionsAsync(
        Guid roleId,
        IReadOnlyList<Guid> permissionIds,
        DateTimeOffset now,
        Guid? actorPlatformUserId,
        CancellationToken cancellationToken);
}


