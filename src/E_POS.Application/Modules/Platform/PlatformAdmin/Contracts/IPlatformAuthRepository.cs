using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

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

    Task<PlatformAuthRefreshContext?> FindRefreshContextByTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken);

    Task<bool> TryRotateRefreshTokenAsync(
        Guid refreshTokenId,
        PlatformRefreshToken replacementRefreshToken,
        string replacementSessionTokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

