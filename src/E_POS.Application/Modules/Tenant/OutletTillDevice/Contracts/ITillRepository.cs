using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface ITillRepository
{
    Task<bool> ActiveOutletExistsAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken);
    Task<bool> TillCodeExistsAsync(Guid tenantId, Guid outletId, string tillCode, Guid? excludeTillId, CancellationToken cancellationToken);
    Task<bool> TillAreaNumberExistsAsync(
        Guid tenantId,
        Guid outletId,
        string tillAreaName,
        int tillNumber,
        Guid? excludeTillId,
        CancellationToken cancellationToken);
    Task<TillListResponse> ListAsync(Guid tenantId, Guid? outletId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<TillResponse?> GetByIdAsync(Guid tenantId, Guid tillId, bool includeDeleted, CancellationToken cancellationToken);
    Task<Till?> GetEditableAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken);
    Task<bool> HasDeviceAssignmentAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken);
    Task AddAsync(Till till, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}


