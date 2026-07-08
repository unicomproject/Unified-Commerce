using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface ITillDeviceAssignmentRepository
{
    Task<TillDeviceAssignmentResponse?> GetByTillAndDeviceAsync(Guid tenantId, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken);
    Task<TillDeviceAssignmentListResponse?> ListByTillAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken);
    Task<bool> ActiveTillExistsAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken);
    Task<bool> ActiveDeviceExistsAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken);
    Task<bool> TillAndDeviceShareOutletAsync(Guid tenantId, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken);
    Task<bool> DeviceAssignedToAnyTillAsync(Guid tenantId, Guid posDeviceId, Guid? excludeTillId, CancellationToken cancellationToken);
    Task<TillDeviceAssignment?> GetEditableAsync(Guid tenantId, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken);
    Task AddAsync(TillDeviceAssignment assignment, CancellationToken cancellationToken);
    Task RevokeAsync(TillDeviceAssignment assignment, DateTimeOffset now, CancellationToken cancellationToken);
}

