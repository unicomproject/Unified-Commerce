using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;

public interface ICustomerProfileRepository
{
    Task<CustomerEntity?> GetCustomerByIdAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken);

    Task UpdateCustomerAsync(
        CustomerEntity customer,
        CancellationToken cancellationToken);
}