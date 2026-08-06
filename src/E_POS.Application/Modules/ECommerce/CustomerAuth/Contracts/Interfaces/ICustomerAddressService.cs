using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;

public interface ICustomerAddressService
{
    Task<ApplicationResult<List<CustomerAddressDto>>> GetAddressesAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken);
    Task<ApplicationResult<CustomerAddressDto>> GetAddressByIdAsync(Guid tenantId, Guid customerId, Guid addressId, CancellationToken cancellationToken);
    Task<ApplicationResult<CustomerAddressDto>> CreateAddressAsync(Guid tenantId, Guid customerId, CreateCustomerAddressRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<CustomerAddressDto>> UpdateAddressAsync(Guid tenantId, Guid customerId, Guid addressId, UpdateCustomerAddressRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteAddressAsync(Guid tenantId, Guid customerId, Guid addressId, CancellationToken cancellationToken);
    Task<ApplicationResult> SetDefaultAddressAsync(Guid tenantId, Guid customerId, Guid addressId, string type, CancellationToken cancellationToken);
}
