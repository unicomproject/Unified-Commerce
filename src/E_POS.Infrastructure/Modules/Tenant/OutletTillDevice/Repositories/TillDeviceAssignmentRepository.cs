using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;

public sealed class TillDeviceAssignmentRepository : ITillDeviceAssignmentRepository
{
    private readonly EPosDbContext _dbContext;

    public TillDeviceAssignmentRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TillOutletContext?> GetTillContextAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken)
    {
        return _dbContext.Tills
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == tillId && x.Status == TillConstants.ActiveStatus)
            .Select(x => new TillOutletContext(x.OutletId))
            .FirstOrDefaultAsync(cancellationToken)!;
    }

    public Task<DeviceOutletContext?> GetDeviceContextAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        return _dbContext.PosDevices
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == posDeviceId && x.Status == PosDeviceConstants.ActiveStatus)
            .Select(x => new DeviceOutletContext(x.OutletId))
            .FirstOrDefaultAsync(cancellationToken)!;
    }

    public Task<bool> DeviceAssignedToAnyTillAsync(Guid tenantId, Guid posDeviceId, Guid? excludeTillId, CancellationToken cancellationToken)
    {
        return _dbContext.TillDeviceAssignments
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.PosDeviceId == posDeviceId &&
                     x.ReleasedAt == null &&
                     (!excludeTillId.HasValue || x.TillId != excludeTillId.Value),
                cancellationToken);
    }

    public Task<bool> TillAssignedToAnyDeviceAsync(Guid tenantId, Guid tillId, Guid? excludePosDeviceId, CancellationToken cancellationToken)
    {
        return _dbContext.TillDeviceAssignments
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.TillId == tillId &&
                     x.ReleasedAt == null &&
                     (!excludePosDeviceId.HasValue || x.PosDeviceId != excludePosDeviceId.Value),
                cancellationToken);
    }

    public Task<TillDeviceAssignmentResponse?> GetActiveByTillAndDeviceAsync(Guid tenantId, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        return BuildAssignmentQuery(tenantId)
            .Where(x => x.TillId == tillId && x.PosDeviceId == posDeviceId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TillDeviceAssignmentListResponse?> ListByTillAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken)
    {
        var tillExists = await _dbContext.Tills
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Id == tillId &&
                     x.Status != TillConstants.DeletedStatus,
                cancellationToken);

        if (!tillExists)
        {
            return null;
        }

        var items = await BuildAssignmentQuery(tenantId)
            .Where(x => x.TillId == tillId)
            .OrderBy(x => x.DeviceCode)
            .ToListAsync(cancellationToken);

        return new TillDeviceAssignmentListResponse(tillId, items);
    }

    public Task<TillDeviceAssignment?> GetEditableAsync(Guid tenantId, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        return _dbContext.TillDeviceAssignments
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.TillId == tillId &&
                     x.PosDeviceId == posDeviceId &&
                     x.ReleasedAt == null,
                cancellationToken);
    }

    public async Task AddAsync(TillDeviceAssignment assignment, CancellationToken cancellationToken)
    {
        _dbContext.TillDeviceAssignments.Add(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseAsync(TillDeviceAssignment assignment, Guid? releasedByTenantUserId, string? releaseReason, DateTimeOffset now, CancellationToken cancellationToken)
    {
        assignment.Release(releasedByTenantUserId, releaseReason, now);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<TillDeviceAssignmentResponse> BuildAssignmentQuery(Guid tenantId)
    {
        return from assignment in _dbContext.TillDeviceAssignments.AsNoTracking()
               where assignment.TenantId == tenantId && assignment.ReleasedAt == null
               join till in _dbContext.Tills.AsNoTracking() on assignment.TillId equals till.Id
               join device in _dbContext.PosDevices.AsNoTracking() on assignment.PosDeviceId equals device.Id
               join outlet in _dbContext.Outlets.AsNoTracking() on assignment.OutletId equals outlet.Id
               where till.TenantId == tenantId &&
                     device.TenantId == tenantId &&
                     outlet.TenantId == tenantId &&
                     till.Status != TillConstants.DeletedStatus &&
                     device.Status != PosDeviceConstants.DeletedStatus
               select new TillDeviceAssignmentResponse(
                   assignment.Id,
                   till.Id,
                   till.TillCode,
                   till.TillName,
                   device.Id,
                   device.DeviceCode,
                   device.DeviceName,
                   outlet.Id,
                   outlet.OutletCode,
                   outlet.OutletName,
                   assignment.AssignedAt,
                   assignment.ReleasedAt);
    }
}
