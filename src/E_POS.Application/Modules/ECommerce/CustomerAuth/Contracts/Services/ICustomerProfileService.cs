using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;

public interface ICustomerProfileService
{
    Task<ApplicationResult<CustomerProfileResponse>> GetProfileAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken);

    Task<ApplicationResult> UpdateProfileAsync(
        Guid tenantId,
        Guid customerId,
        CustomerProfileUpdateRequest request,
        CancellationToken cancellationToken);
}
