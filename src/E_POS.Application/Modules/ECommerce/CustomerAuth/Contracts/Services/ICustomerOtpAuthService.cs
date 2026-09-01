using System.Net;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;

public interface ICustomerOtpAuthService
{
    Task<ApplicationResult> RequestOtpAsync(
        Guid tenantId,
        CustomerRequestOtpRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CustomerAuthTokenResult>> VerifyOtpAsync(
        Guid tenantId,
        CustomerVerifyOtpRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);
}
