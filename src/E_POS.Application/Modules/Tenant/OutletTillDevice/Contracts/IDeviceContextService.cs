using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface IDeviceContextService
{
    Task<ApplicationResult<CurrentDeviceResponseDto>> GetCurrentDeviceAsync(
        TenantRequestContext context,
        string? deviceFingerprint,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CurrentDeviceResponseDto>> ActivateDeviceAsync(
        TenantRequestContext context,
        ActivateDeviceRequest request,
        CancellationToken cancellationToken);
}
