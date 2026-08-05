using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;

public interface ICustomerRegistrationRepository
{
    Task<bool> TenantIsActiveAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<CustomerLoginAccount?> FindAccountByEmailAsync(
        Guid tenantId,
        string normalizedEmail,
        bool trackAccount,
        CancellationToken cancellationToken);

    Task<bool> RegisterCustomerAsync(
        CustomerEntity customer,
        CustomerAuthAccount account,
        CustomerVerificationOtp verificationOtp,
        IReadOnlyCollection<CustomerConsent> consents,
        CancellationToken cancellationToken);
}