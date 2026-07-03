using E_POS.Application.Modules.OutletTillDevice.Contracts;
using E_POS.Application.Modules.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.OutletTillDevice.Constants;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.OutletTillDevice.Repositories;

public sealed class TillDeviceAssignmentRepository : ITillDeviceAssignmentRepository
{
    private readonly EPosDbContext _dbContext;

    public TillDeviceAssignmentRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ActiveTillExistsAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken)
    {
        return _dbContext.Tills
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Id == tillId &&
                     x.Status == TillConstants.ActiveStatus,
                cancellationToken);
    }

    public Task<bool> ActiveDeviceExistsAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        return _dbContext.PosDevices
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Id == posDeviceId &&
                     x.Status == PosDeviceConstants.ActiveStatus,
                cancellationToken);
    }

    public Task<bool> TillAndDeviceShareOutletAsync(Guid tenantId, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        return (from till in _dbContext.Tills.AsNoTracking()
                where till.OutletId.HasValue
                join device in _dbContext.PosDevices.AsNoTracking() on till.OutletId.GetValueOrDefault() equals device.OutletId
                where till.TenantId == tenantId &&
                      device.TenantId == tenantId &&
                      till.Id == tillId &&
                      device.Id == posDeviceId
                select till.Id)
            .AnyAsync(cancellationToken);
    }

    public Task<bool> DeviceAssignedToAnyTillAsync(Guid tenantId, Guid posDeviceId, Guid? excludeTillId, CancellationToken cancellationToken)
    {
        return (from assignment in _dbContext.TillDeviceAssignments.AsNoTracking()
                where assignment.TillId.HasValue && assignment.PosDeviceId.HasValue && assignment.Status == TillDeviceAssignmentConstants.ActiveStatus
                join till in _dbContext.Tills.AsNoTracking() on assignment.TillId.GetValueOrDefault() equals till.Id
                where till.TenantId == tenantId &&
                      assignment.PosDeviceId.GetValueOrDefault() == posDeviceId &&
                      (!excludeTillId.HasValue || till.Id != excludeTillId.Value)
                select assignment.Id)
            .AnyAsync(cancellationToken);
    }

    public Task<TillDeviceAssignmentResponse?> GetByTillAndDeviceAsync(Guid tenantId, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken)
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
        return (from assignment in _dbContext.TillDeviceAssignments
                where assignment.TillId.HasValue && assignment.PosDeviceId.HasValue && assignment.Status == TillDeviceAssignmentConstants.ActiveStatus
                join till in _dbContext.Tills on assignment.TillId.GetValueOrDefault() equals till.Id
                join device in _dbContext.PosDevices on assignment.PosDeviceId.GetValueOrDefault() equals device.Id
                where till.TenantId == tenantId &&
                      device.TenantId == tenantId &&
                      assignment.TillId.GetValueOrDefault() == tillId &&
                      assignment.PosDeviceId.GetValueOrDefault() == posDeviceId
                select assignment)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(TillDeviceAssignment assignment, CancellationToken cancellationToken)
    {
        _dbContext.TillDeviceAssignments.Add(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(TillDeviceAssignment assignment, DateTimeOffset now, CancellationToken cancellationToken)
    {
        assignment.Revoke(now);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<TillDeviceAssignmentResponse> BuildAssignmentQuery(Guid tenantId)
    {
        return from assignment in _dbContext.TillDeviceAssignments.AsNoTracking()
               where assignment.TillId.HasValue && assignment.PosDeviceId.HasValue && assignment.Status == TillDeviceAssignmentConstants.ActiveStatus
               join till in _dbContext.Tills.AsNoTracking() on assignment.TillId.GetValueOrDefault() equals till.Id
               where till.OutletId.HasValue
               join device in _dbContext.PosDevices.AsNoTracking() on assignment.PosDeviceId.GetValueOrDefault() equals device.Id
               join outlet in _dbContext.Outlets.AsNoTracking() on till.OutletId.GetValueOrDefault() equals outlet.Id
               where till.TenantId == tenantId &&
                     device.TenantId == tenantId &&
                     outlet.TenantId == tenantId &&
                     device.OutletId == outlet.Id &&
                     till.Status != TillConstants.DeletedStatus &&
                     device.Status != PosDeviceConstants.DeletedStatus
               select new TillDeviceAssignmentResponse(
                   assignment.Id,
                   till.Id,
                   till.TillCode,
                   till.Name,
                   device.Id,
                   device.DeviceCode,
                   device.Name,
                   outlet.Id,
                   outlet.OutletCode,
                   outlet.Name,
                   assignment.EffectiveFrom,
                   assignment.CreatedAt,
                   assignment.UpdatedAt);
    }
}