using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Domain.Modules.PlatformAdministration.Entities;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformUserRepository
{
    Task<PlatformUserListResponse> GetUsersAsync(CancellationToken cancellationToken);

    Task<PlatformUserDetailResponse?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<PlatformUser?> GetUserEntityByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(
        string normalizedEmail,
        Guid? excludingUserId,
        CancellationToken cancellationToken);

    Task AddUserWithRolesAsync(
        PlatformUser user,
        IReadOnlyList<Guid> roleIds,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task UpdateUserAsync(PlatformUser user, CancellationToken cancellationToken);

    Task ReplaceUserRolesAsync(
        Guid userId,
        IReadOnlyList<Guid> roleIds,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ResolvedPlatformRole>> ResolveActiveRolesAsync(
        IReadOnlyList<Guid>? roleIds,
        IReadOnlyList<string>? roleCodes,
        CancellationToken cancellationToken);

    Task<bool> UserHasActiveRoleCodeAsync(
        Guid userId,
        string roleCode,
        CancellationToken cancellationToken);

    Task<int> CountActiveSuperAdministratorsAsync(
        Guid? excludingUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetUserActiveRoleCodesAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
