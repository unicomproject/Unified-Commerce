using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformRoleService
{
    Task<ApplicationResult<PlatformRoleListResponse>> GetRolesAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformRoleDetailResponse>> GetRoleAsync(
        Guid roleId,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformRoleDetailResponse>> CreateRoleAsync(
        CreatePlatformRoleRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformRoleDetailResponse>> UpdateRoleAsync(
        Guid roleId,
        UpdatePlatformRoleRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformRolePermissionsResponse>> GetRolePermissionsAsync(
        Guid roleId,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformRolePermissionsResponse>> UpdateRolePermissionsAsync(
        Guid roleId,
        UpdatePlatformRolePermissionsRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken);
}

