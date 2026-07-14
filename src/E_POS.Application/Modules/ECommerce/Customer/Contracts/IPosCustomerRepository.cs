using E_POS.Application.Modules.ECommerce.Customer.Dtos;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.Customer.Contracts;

public interface IPosCustomerRepository
{
    Task<bool> NormalizedPhoneExistsAsync(
        Guid tenantId,
        string normalizedPhone,
        CancellationToken cancellationToken);

    Task<bool> NormalizedEmailExistsAsync(
        Guid tenantId,
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<bool> AddAsync(CustomerEntity customer, CancellationToken cancellationToken);

    Task<PosCustomerListResponseDto> ListAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
