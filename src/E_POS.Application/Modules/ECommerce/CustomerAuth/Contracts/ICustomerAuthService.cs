using System.Net;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts;

public interface ICustomerAuthService
{
    Task<ApplicationResult> RegisterAsync(
        Guid tenantId,
        CustomerRegisterRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<ApplicationResult> VerifyEmailAsync(
        Guid tenantId,
        CustomerVerifyEmailRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ResendEmailVerificationAsync(
        Guid tenantId,
        CustomerResendEmailVerificationRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ForgotPasswordAsync(
        Guid tenantId,
        CustomerForgotPasswordRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ResetPasswordAsync(
        Guid tenantId,
        CustomerResetPasswordRequest request,
        CancellationToken cancellationToken);

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

public interface ICustomerPasswordResetLinkBuilder
{
    string BuildResetUrl(string email, string rawToken);
}