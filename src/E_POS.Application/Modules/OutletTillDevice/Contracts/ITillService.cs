using E_POS.Application.Common.Models;
using E_POS.Application.Modules.OutletTillDevice.Dtos;

namespace E_POS.Application.Modules.OutletTillDevice.Contracts;

public interface ITillService
{
    Task<ApplicationResult<TillResponse>> CreateAsync(TenantRequestContext context, TillCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<TillListResponse>> ListAsync(TenantRequestContext context, Guid? outletId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ApplicationResult<TillResponse>> GetByIdAsync(TenantRequestContext context, Guid tillId, CancellationToken cancellationToken);
    Task<ApplicationResult<TillResponse>> UpdateAsync(TenantRequestContext context, Guid tillId, TillUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid tillId, CancellationToken cancellationToken);
}