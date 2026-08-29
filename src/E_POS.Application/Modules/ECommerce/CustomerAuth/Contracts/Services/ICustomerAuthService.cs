using System.Net;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;

public interface ICustomerAuthService
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
    Task<ApplicationResult<CustomerAuthTokenResult>> GoogleLoginAsync(
        Guid tenantId,
        CustomerGoogleLoginRequest request,
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

