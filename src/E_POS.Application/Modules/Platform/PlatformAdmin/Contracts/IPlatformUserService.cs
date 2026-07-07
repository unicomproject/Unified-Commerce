using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformUserService
{
    Task<ApplicationResult<PlatformUserListResponse>> GetUsersAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformUserDetailResponse>> GetUserAsync(
        Guid userId,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformUserDetailResponse>> CreateUserAsync(
        CreatePlatformUserRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformUserDetailResponse>> UpdateUserAsync(
        Guid userId,
        UpdatePlatformUserRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformUserDetailResponse>> AssignRolesAsync(
        Guid userId,
        AssignPlatformUserRolesRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken);
}

