using E_POS.Domain.Modules.PlatformAdministration.Entities;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformAuthRepository
{
    Task<PlatformUser?> FindUserByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetActivePermissionCodesAsync(Guid platformUserId, CancellationToken cancellationToken);

    Task SaveFailedLoginAuditAsync(PlatformLoginAudit audit, CancellationToken cancellationToken);

    Task SaveSuccessfulLoginAsync(
        PlatformAuthSession session,
        PlatformRefreshToken refreshToken,
        PlatformLoginAudit audit,
        CancellationToken cancellationToken);
}
