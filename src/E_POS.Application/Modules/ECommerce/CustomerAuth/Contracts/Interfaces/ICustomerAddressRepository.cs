using E_POS.Domain.Modules.ECommerce.Customer.Entities;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;

public interface ICustomerAddressRepository
{
    Task<List<CustomerAddress>> GetAddressesAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken);
    Task<CustomerAddress?> GetAddressByIdAsync(Guid tenantId, Guid customerId, Guid addressId, CancellationToken cancellationToken);
    Task<List<CustomerAddress>> GetAddressesByTypeAsync(Guid tenantId, Guid customerId, string type, CancellationToken cancellationToken);
    Task<List<CustomerAddress>> GetDefaultShippingAddressesAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken);
    Task<List<CustomerAddress>> GetDefaultBillingAddressesAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken);
    void AddAddress(CustomerAddress address);
    void RemoveAddress(CustomerAddress address);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
