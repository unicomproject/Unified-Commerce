using System.Net;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;

public interface ICustomerRegistrationService
{
    Task<ApplicationResult> RegisterAsync(
        Guid tenantId,
        CustomerRegisterRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);
}
