using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;

public sealed class PosDeviceRepository : IPosDeviceRepository
{
    private readonly EPosDbContext _dbContext;

    public PosDeviceRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ActiveOutletExistsAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
    {
        return _dbContext.Outlets
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Id == outletId &&
                     x.Status == OutletConstants.ActiveStatus,
                cancellationToken);
    }

    public Task<bool> DeviceSerialNumberExistsAsync(string deviceSerialNumber, Guid? excludePosDeviceId, CancellationToken cancellationToken)
    {
        return _dbContext.PosDevices
            .AsNoTracking()
            .AnyAsync(
                x => x.DeviceSerialNumber == deviceSerialNumber &&
                     (!excludePosDeviceId.HasValue || x.Id != excludePosDeviceId.Value),
                cancellationToken);
    }

    public async Task<PosDeviceListResponse> ListAsync(
        Guid tenantId,
        Guid? outletId,
        int pageNumber,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var assignedTillQuery = from assignment in _dbContext.TillDeviceAssignments.AsNoTracking()
                                where assignment.TillId.HasValue && assignment.PosDeviceId.HasValue && assignment.Status == TillDeviceAssignmentConstants.ActiveStatus
                                join till in _dbContext.Tills.AsNoTracking() on assignment.TillId.GetValueOrDefault() equals till.Id
                                where till.TenantId == tenantId && till.Status != TillConstants.DeletedStatus
                                select new
                                {
                                    PosDeviceId = assignment.PosDeviceId.GetValueOrDefault(),
                                    TillId = till.Id,
                                    OutletId = till.OutletId.GetValueOrDefault(),
                                    till.TillCode,
                                    till.Name
                                };

        var query = from device in _dbContext.PosDevices.AsNoTracking()
                    join outlet in _dbContext.Outlets.AsNoTracking() on device.OutletId equals outlet.Id
                    join assignedTill in assignedTillQuery on new { DeviceId = device.Id, device.OutletId } equals new { DeviceId = assignedTill.PosDeviceId, assignedTill.OutletId } into assignedTills
                    from assignedTill in assignedTills.DefaultIfEmpty()
                    where device.TenantId == tenantId &&
                          outlet.TenantId == tenantId &&
                          device.Status != PosDeviceConstants.DeletedStatus
                    select new { device, outlet, assignedTill };

        if (outletId.HasValue)
        {
            query = query.Where(x => x.device.OutletId == outletId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.device.Name, pattern) ||
                                     EF.Functions.ILike(x.device.DeviceCode, pattern) ||
                                     EF.Functions.ILike(x.device.DeviceSerialNumber, pattern) ||
                                     EF.Functions.ILike(x.outlet.Name, pattern) ||
                                     EF.Functions.ILike(x.outlet.OutletCode, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.outlet.OutletCode)
            .ThenBy(x => x.device.DeviceCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PosDeviceSummaryResponse(
                x.device.Id,
                x.device.OutletId,
                x.outlet.OutletCode,
                x.outlet.Name,
                x.device.DeviceCode,
                x.device.Name,
                x.device.DeviceSerialNumber,
                x.device.Status,
                x.assignedTill == null ? null : x.assignedTill.TillId,
                x.assignedTill == null ? null : x.assignedTill.TillCode,
                x.assignedTill == null ? null : x.assignedTill.Name,
                x.device.CreatedAt,
                x.device.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PosDeviceListResponse(items, pageNumber, pageSize, totalCount);
    }

    public async Task<PosDeviceResponse?> GetByIdAsync(Guid tenantId, Guid posDeviceId, bool includeDeleted, CancellationToken cancellationToken)
    {
        var assignedTillQuery = from assignment in _dbContext.TillDeviceAssignments.AsNoTracking()
                                where assignment.TillId.HasValue && assignment.PosDeviceId.HasValue && assignment.Status == TillDeviceAssignmentConstants.ActiveStatus
                                join till in _dbContext.Tills.AsNoTracking() on assignment.TillId.GetValueOrDefault() equals till.Id
                                where till.TenantId == tenantId && till.Status != TillConstants.DeletedStatus
                                select new
                                {
                                    PosDeviceId = assignment.PosDeviceId.GetValueOrDefault(),
                                    TillId = till.Id,
                                    OutletId = till.OutletId.GetValueOrDefault(),
                                    till.TillCode,
                                    till.Name
                                };

        return await (from device in _dbContext.PosDevices.AsNoTracking()
                      join outlet in _dbContext.Outlets.AsNoTracking() on device.OutletId equals outlet.Id
                      join assignedTill in assignedTillQuery on new { DeviceId = device.Id, device.OutletId } equals new { DeviceId = assignedTill.PosDeviceId, assignedTill.OutletId } into assignedTills
                      from assignedTill in assignedTills.DefaultIfEmpty()
                      where device.TenantId == tenantId &&
                            device.Id == posDeviceId &&
                            outlet.TenantId == tenantId &&
                            (includeDeleted || device.Status != PosDeviceConstants.DeletedStatus)
                      select new PosDeviceResponse(
                          device.Id,
                          device.OutletId,
                          outlet.OutletCode,
                          outlet.Name,
                          device.DeviceCode,
                          device.Name,
                          device.DeviceSerialNumber,
                          device.Status,
                          assignedTill == null ? null : assignedTill.TillId,
                          assignedTill == null ? null : assignedTill.TillCode,
                          assignedTill == null ? null : assignedTill.Name,
                          device.CreatedAt,
                          device.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<PosDevice?> GetEditableAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        return _dbContext.PosDevices
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == posDeviceId &&
                     x.Status != PosDeviceConstants.DeletedStatus,
                cancellationToken);
    }

    public Task<bool> HasTillAssignmentAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        return _dbContext.TillDeviceAssignments
            .AsNoTracking()
            .AnyAsync(
                assignment => assignment.PosDeviceId == posDeviceId && assignment.Status == TillDeviceAssignmentConstants.ActiveStatus &&
                              _dbContext.Tills.Any(till => till.Id == assignment.TillId && till.TenantId == tenantId),
                cancellationToken);
    }

    public async Task AddAsync(PosDevice posDevice, CancellationToken cancellationToken)
    {
        _dbContext.PosDevices.Add(posDevice);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}


