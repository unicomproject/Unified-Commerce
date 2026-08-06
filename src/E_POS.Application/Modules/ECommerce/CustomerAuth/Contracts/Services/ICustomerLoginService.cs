using System.Net;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;

public interface ICustomerLoginService
{
    Task<ApplicationResult<CustomerAuthTokenResult>> LoginAsync(
        Guid tenantId,
        CustomerLoginRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);
}
