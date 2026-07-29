using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts;

public interface ICustomerAuthRepository
{
    Task<bool> TenantIsActiveAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<bool> NormalizedEmailExistsAsync(
        Guid tenantId,
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<CustomerLoginAccount?> FindAccountByEmailAsync(
        Guid tenantId,
        string normalizedEmail,
        bool trackAccount,
        CancellationToken cancellationToken);

    Task<CustomerLoginAccount?> FindLoginAccountAsync(
        Guid tenantId,
        string normalizedEmail,
        string normalizedPhone,
        CancellationToken cancellationToken);

    Task<bool> RegisterCustomerAsync(
        CustomerEntity customer,
        CustomerAuthAccount account,
        CustomerVerificationOtp verificationOtp,
        IReadOnlyCollection<CustomerConsent> consents,
        CancellationToken cancellationToken);

    Task SaveEmailVerificationOtpAsync(
        CustomerVerificationOtp verificationOtp,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<CustomerEmailVerificationContext?> FindPendingEmailVerificationAsync(
        Guid tenantId,
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task SaveEmailVerificationAsync(
        CustomerVerificationOtp verificationOtp,
        CustomerAuthAccount account,
        CancellationToken cancellationToken);

    Task SavePasswordResetTokenAsync(
        CustomerPasswordResetToken resetToken,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<CustomerPasswordResetContext?> FindActivePasswordResetAsync(
        Guid tenantId,
        string normalizedEmail,
        string tokenHash,
        CancellationToken cancellationToken);

    Task SavePasswordResetAsync(
        CustomerPasswordResetToken resetToken,
        CustomerAuthAccount account,
        DateTimeOffset now,
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

    Task<CustomerEntity?> GetCustomerByIdAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken);

    Task UpdateCustomerAsync(
        CustomerEntity customer,
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

public sealed record CustomerEmailVerificationContext(
    CustomerVerificationOtp VerificationOtp,
    CustomerAuthAccount Account,
    string? Email,
    string DisplayName,
    string CustomerStatus,
    string TenantStatus);

public sealed record CustomerPasswordResetContext(
    CustomerPasswordResetToken ResetToken,
    CustomerAuthAccount Account,
    string? Email,
    string DisplayName,
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