using System.Net;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;

public interface ICustomerEmailVerificationService
{
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

    Task<ApplicationResult> SendVerificationForExistingAccountAsync(
        CustomerLoginAccount account,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);
}
