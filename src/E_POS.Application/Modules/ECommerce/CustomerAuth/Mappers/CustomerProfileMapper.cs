using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Mappers;

public static class CustomerProfileMapper
{
    public static CustomerProfileResponse ToResponse(CustomerEntity customer) => new()
    {
        FirstName = customer.FirstName ?? string.Empty,
        LastName = customer.LastName ?? string.Empty,
        Email = customer.Email ?? string.Empty,
        Phone = customer.Phone ?? string.Empty
    };
}
