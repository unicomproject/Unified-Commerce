using E_POS.Domain.Modules.ECommerce.Customer.Entities;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;

public interface ICustomerLoginRepository
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
}