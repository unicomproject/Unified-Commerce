using E_POS.Domain.Modules.ECommerce.Customer.Entities;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;

public interface ICustomerPasswordResetRepository
{
    Task<bool> TenantIsActiveAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<CustomerLoginAccount?> FindAccountByEmailAsync(
        Guid tenantId,
        string normalizedEmail,
        bool trackAccount,
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
}