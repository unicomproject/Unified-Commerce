using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;

namespace E_POS.Application.Modules.Tenant.TenantAuth.Contracts;

public interface ITenantAuthRepository
{
    Task<TenantLoginAccount?> FindLoginAccountByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetActivePermissionCodesAsync(
        Guid tenantUserId,
        Guid tenantId,
        CancellationToken cancellationToken);

    Task SaveFailedLoginAuditAsync(TenantLoginAudit audit, CancellationToken cancellationToken);

    Task SaveFailedCredentialAttemptAsync(
        TenantLoginAudit audit,
        DateTimeOffset failedAttemptWindowStart,
        int maxFailedAttempts,
        CancellationToken cancellationToken);

    Task SaveSuccessfulLoginAsync(
        TenantAuthSession session,
        TenantRefreshToken refreshToken,
        TenantLoginAudit audit,
        CancellationToken cancellationToken);

    Task RevokeCurrentSessionAsync(
        Guid tenantUserId,
        Guid tenantId,
        Guid sessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

