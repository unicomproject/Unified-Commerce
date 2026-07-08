using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface ITillDeviceAssignmentService
{
    Task<ApplicationResult<TillDeviceAssignmentResponse>> AssignAsync(TenantRequestContext context, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken);
    Task<ApplicationResult<TillDeviceAssignmentListResponse>> ListByTillAsync(TenantRequestContext context, Guid tillId, CancellationToken cancellationToken);
    Task<ApplicationResult> RemoveAsync(TenantRequestContext context, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken);
}
