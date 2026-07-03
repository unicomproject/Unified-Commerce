using E_POS.Application.Modules.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.OutletTillDevice.Entities;

namespace E_POS.Application.Modules.OutletTillDevice.Contracts;

public interface IPosDeviceRepository
{
    Task<bool> ActiveOutletExistsAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken);
    Task<bool> DeviceSerialNumberExistsAsync(string deviceSerialNumber, Guid? excludePosDeviceId, CancellationToken cancellationToken);
    Task<PosDeviceListResponse> ListAsync(Guid tenantId, Guid? outletId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<PosDeviceResponse?> GetByIdAsync(Guid tenantId, Guid posDeviceId, bool includeDeleted, CancellationToken cancellationToken);
    Task<PosDevice?> GetEditableAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken);
    Task<bool> HasTillAssignmentAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken);
    Task AddAsync(PosDevice posDevice, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}