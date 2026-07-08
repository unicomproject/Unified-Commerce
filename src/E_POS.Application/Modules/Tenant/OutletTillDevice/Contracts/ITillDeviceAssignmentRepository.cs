using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface ITillDeviceAssignmentRepository
{
    Task<TillDeviceAssignmentResponse?> GetActiveByTillAndDeviceAsync(Guid tenantId, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken);
    Task<TillDeviceAssignmentListResponse?> ListByTillAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken);
    Task<TillOutletContext?> GetTillContextAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken);
    Task<DeviceOutletContext?> GetDeviceContextAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken);
    Task<bool> DeviceAssignedToAnyTillAsync(Guid tenantId, Guid posDeviceId, Guid? excludeTillId, CancellationToken cancellationToken);
    Task<bool> TillAssignedToAnyDeviceAsync(Guid tenantId, Guid tillId, Guid? excludePosDeviceId, CancellationToken cancellationToken);
    Task<TillDeviceAssignment?> GetEditableAsync(Guid tenantId, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken);
    Task AddAsync(TillDeviceAssignment assignment, CancellationToken cancellationToken);
    Task ReleaseAsync(TillDeviceAssignment assignment, Guid? releasedByTenantUserId, string? releaseReason, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed record TillOutletContext(Guid OutletId);
public sealed record DeviceOutletContext(Guid OutletId);
