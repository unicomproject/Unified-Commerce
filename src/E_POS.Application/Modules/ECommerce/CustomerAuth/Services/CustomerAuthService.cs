using System.Net;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.Customer.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services;

public sealed class CustomerAuthService : ICustomerAuthService
{
    private readonly ICustomerRegistrationService _registrationService;
    private readonly ICustomerEmailVerificationService _emailVerificationService;
    private readonly ICustomerPasswordResetService _passwordResetService;
    private readonly ICustomerLoginService _loginService;
    private readonly ICustomerGoogleAuthService _googleAuthService;
    private readonly ICustomerSessionService _sessionService;
    private readonly ICustomerProfileService _profileService;

    public CustomerAuthService(
        ICustomerRegistrationService registrationService,
        ICustomerEmailVerificationService emailVerificationService,
        ICustomerPasswordResetService passwordResetService,
        ICustomerLoginService loginService,
        ICustomerGoogleAuthService googleAuthService,
        ICustomerSessionService sessionService,
        ICustomerProfileService profileService)
    {
        _registrationService = registrationService;
        _emailVerificationService = emailVerificationService;
        _passwordResetService = passwordResetService;
        _loginService = loginService;
        _googleAuthService = googleAuthService;
        _sessionService = sessionService;
        _profileService = profileService;
    }

    public Task<ApplicationResult> RegisterAsync(
        Guid tenantId,
        CustomerRegisterRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken) =>
        _registrationService.RegisterAsync(tenantId, request, ipAddress, userAgent, cancellationToken);

    public Task<ApplicationResult> VerifyEmailAsync(
        Guid tenantId,
        CustomerVerifyEmailRequest request,
        CancellationToken cancellationToken) =>
        _emailVerificationService.VerifyEmailAsync(tenantId, request, cancellationToken);

    public Task<ApplicationResult> ResendEmailVerificationAsync(
        Guid tenantId,
        CustomerResendEmailVerificationRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken) =>
        _emailVerificationService.ResendEmailVerificationAsync(
            tenantId,
            request,
            ipAddress,
            userAgent,
            cancellationToken);

    public Task<ApplicationResult> ForgotPasswordAsync(
        Guid tenantId,
        CustomerForgotPasswordRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken) =>
        _passwordResetService.ForgotPasswordAsync(tenantId, request, ipAddress, userAgent, cancellationToken);

    public Task<ApplicationResult> ResetPasswordAsync(
        Guid tenantId,
        CustomerResetPasswordRequest request,
        CancellationToken cancellationToken) =>
        _passwordResetService.ResetPasswordAsync(tenantId, request, cancellationToken);

    public Task<ApplicationResult<CustomerAuthTokenResult>> LoginAsync(
        Guid tenantId,
        CustomerLoginRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken) =>
        _loginService.LoginAsync(tenantId, request, ipAddress, userAgent, cancellationToken);

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
