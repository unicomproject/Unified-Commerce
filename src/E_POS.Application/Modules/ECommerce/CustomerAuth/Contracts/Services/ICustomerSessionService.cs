using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;

public interface ICustomerSessionService
{
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
