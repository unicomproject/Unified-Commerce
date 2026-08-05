using System.Net;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;

public interface ICustomerPasswordResetService
{
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
}
