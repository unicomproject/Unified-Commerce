using System.Net;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.Customer.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services;

public sealed class CustomerAuthService : ICustomerAuthService
{
    private readonly ICustomerOtpAuthService _otpService;
    private readonly ICustomerGoogleAuthService _googleAuthService;
    private readonly ICustomerSessionService _sessionService;
    private readonly ICustomerProfileService _profileService;

    public CustomerAuthService(
        ICustomerOtpAuthService otpService,
        ICustomerGoogleAuthService googleAuthService,
        ICustomerSessionService sessionService,
        ICustomerProfileService profileService)
    {
        _otpService = otpService;
        _googleAuthService = googleAuthService;
        _sessionService = sessionService;
        _profileService = profileService;
    }

    public Task<ApplicationResult> RequestOtpAsync(
        Guid tenantId,
        CustomerRequestOtpRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken) =>
        _otpService.RequestOtpAsync(tenantId, request, ipAddress, userAgent, cancellationToken);

    public Task<ApplicationResult<CustomerAuthTokenResult>> VerifyOtpAsync(
        Guid tenantId,
        CustomerVerifyOtpRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken) =>
        _otpService.VerifyOtpAsync(tenantId, request, ipAddress, userAgent, cancellationToken);

    public Task<ApplicationResult<CustomerAuthTokenResult>> GoogleLoginAsync(
        Guid tenantId,
        CustomerGoogleLoginRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken) =>
        _googleAuthService.GoogleLoginAsync(tenantId, request, ipAddress, userAgent, cancellationToken);

    public Task<ApplicationResult<CustomerAuthTokenResult>> RefreshAsync(
        Guid tenantId,
        string refreshToken,
        CancellationToken cancellationToken) =>
        _sessionService.RefreshAsync(tenantId, refreshToken, cancellationToken);

    public Task<ApplicationResult> LogoutAsync(
        Guid tenantId,
        Guid customerId,
        Guid sessionId,
        CancellationToken cancellationToken) =>
        _sessionService.LogoutAsync(tenantId, customerId, sessionId, cancellationToken);

    public Task<ApplicationResult<CustomerProfileResponse>> GetProfileAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken) =>
        _profileService.GetProfileAsync(tenantId, customerId, cancellationToken);

    public Task<ApplicationResult> UpdateProfileAsync(
        Guid tenantId,
        Guid customerId,
        CustomerProfileUpdateRequest request,
        CancellationToken cancellationToken) =>
        _profileService.UpdateProfileAsync(tenantId, customerId, request, cancellationToken);
}
