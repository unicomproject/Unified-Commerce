using E_POS.Domain.Modules.PlatformAdministration.Entities;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformAuthRepository
{
    Task<PlatformUser?> FindUserByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetActivePermissionCodesAsync(Guid platformUserId, CancellationToken cancellationToken);

    Task SaveFailedLoginAuditAsync(PlatformLoginAudit audit, CancellationToken cancellationToken);

    Task SaveFailedCredentialAttemptAsync(
        PlatformLoginAudit audit,
        DateTimeOffset failedAttemptWindowStart,
        int maxFailedAttempts,
        CancellationToken cancellationToken);

    Task SaveSuccessfulLoginAsync(
        PlatformAuthSession session,
        PlatformRefreshToken refreshToken,
        PlatformLoginAudit audit,
        CancellationToken cancellationToken);

    Task RevokeCurrentSessionAsync(
        Guid platformUserId,
        Guid sessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}