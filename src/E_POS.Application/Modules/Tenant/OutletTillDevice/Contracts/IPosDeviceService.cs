using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface IPosDeviceService
{
    Task<ApplicationResult<PosDeviceResponse>> CreateAsync(TenantRequestContext context, PosDeviceCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<PosDeviceListResponse>> ListAsync(TenantRequestContext context, Guid? outletId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ApplicationResult<PosDeviceResponse>> GetByIdAsync(TenantRequestContext context, Guid posDeviceId, CancellationToken cancellationToken);
    Task<ApplicationResult<PosDeviceResponse>> UpdateAsync(TenantRequestContext context, Guid posDeviceId, PosDeviceUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid posDeviceId, CancellationToken cancellationToken);
}
