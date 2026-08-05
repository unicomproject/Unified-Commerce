using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;

public interface ICustomerExternalAuthRepository
{
    Task<bool> TenantIsActiveAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<CustomerLoginAccount?> FindAccountByEmailAsync(
        Guid tenantId,
        string normalizedEmail,
        bool trackAccount,
        CancellationToken cancellationToken);

    Task<CustomerExternalLoginAccount?> FindExternalLoginAccountAsync(
        Guid tenantId,
        string providerCode,
        string providerSubject,
        bool trackAccount,
        bool trackExternalAccount,
        CancellationToken cancellationToken);

    Task<bool> RegisterExternalCustomerAsync(
        CustomerEntity customer,
        CustomerAuthAccount account,
        CustomerExternalAuthAccount externalAccount,
        IReadOnlyCollection<CustomerConsent> consents,
        CustomerAuthSession session,
        CustomerRefreshToken refreshToken,
        CancellationToken cancellationToken);

    Task<bool> LinkExternalAccountAndSaveLoginAsync(
        CustomerAuthAccount account,
        CustomerExternalAuthAccount externalAccount,
        CustomerAuthSession session,
        CustomerRefreshToken refreshToken,
        CancellationToken cancellationToken);

    Task SaveSuccessfulExternalLoginAsync(
        CustomerAuthAccount account,
        CustomerExternalAuthAccount externalAccount,
        CustomerAuthSession session,
        CustomerRefreshToken refreshToken,
        CancellationToken cancellationToken);
}