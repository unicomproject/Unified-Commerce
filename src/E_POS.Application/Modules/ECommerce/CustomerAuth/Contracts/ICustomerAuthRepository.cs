using E_POS.Domain.Modules.ECommerce.Customer.Entities;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts;

public interface ICustomerAuthRepository
{
    Task<CustomerLoginAccount?> FindLoginAccountAsync(
        Guid tenantId,
        string normalizedEmail,
        string normalizedPhone,
        CancellationToken cancellationToken);

    Task SaveFailedLoginAsync(CustomerAuthAccount account, CancellationToken cancellationToken);

    Task SaveSuccessfulLoginAsync(
        CustomerAuthAccount account,
        CustomerAuthSession session,
        CustomerRefreshToken refreshToken,
        CancellationToken cancellationToken);

    Task<CustomerRefreshRotationResult> RotateRefreshTokenAsync(
        Guid tenantId,
        string currentTokenHash,
        Guid replacementTokenId,
        string replacementTokenHash,
        DateTimeOffset replacementExpiresAt,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> RevokeSessionAsync(
        Guid tenantId,
        Guid customerId,
        Guid sessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record CustomerLoginAccount(
    CustomerAuthAccount Account,
    Guid CustomerId,
    Guid TenantId,
    string DisplayName,
    string? Email,
    string? Phone,
    string CustomerStatus,
    string TenantStatus);

public enum CustomerRefreshRotationStatus
{
    Succeeded,
    Invalid,
    Reused,
    AccountUnavailable
}

public sealed record CustomerRefreshRotationResult(
    CustomerRefreshRotationStatus Status,
    CustomerLoginAccount? Account,
    Guid? SessionId);
