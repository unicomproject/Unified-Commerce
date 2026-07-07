using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface IOutletService
{
    Task<ApplicationResult<OutletResponse>> CreateAsync(TenantRequestContext context, OutletCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<OutletListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ApplicationResult<OutletResponse>> GetByIdAsync(TenantRequestContext context, Guid outletId, CancellationToken cancellationToken);
    Task<ApplicationResult<OutletResponse>> UpdateAsync(TenantRequestContext context, Guid outletId, OutletUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid outletId, CancellationToken cancellationToken);
}
