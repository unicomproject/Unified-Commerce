using System.Net;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts;

public interface ICustomerAuthService
{
    Task<ApplicationResult<CustomerAuthTokenResult>> LoginAsync(
        Guid tenantId,
        CustomerLoginRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CustomerAuthTokenResult>> RefreshAsync(
        Guid tenantId,
        string refreshToken,
        CancellationToken cancellationToken);

    Task<ApplicationResult> LogoutAsync(
        Guid tenantId,
        Guid customerId,
        Guid sessionId,
        CancellationToken cancellationToken);
}
