using E_POS.Domain.Modules.ECommerce.Customer.Entities;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;

public interface ICustomerEmailVerificationRepository
{
    Task<bool> TenantIsActiveAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<CustomerLoginAccount?> FindAccountByEmailAsync(
        Guid tenantId,
        string normalizedEmail,
        bool trackAccount,
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
}