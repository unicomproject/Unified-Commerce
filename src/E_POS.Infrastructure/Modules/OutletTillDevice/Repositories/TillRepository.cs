using E_POS.Application.Modules.OutletTillDevice.Contracts;
using E_POS.Application.Modules.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.OutletTillDevice.Constants;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.OutletTillDevice.Repositories;

public sealed class TillRepository : ITillRepository
{
    private readonly EPosDbContext _dbContext;

    public TillRepository(EPosDbContext dbContext)
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

    public Task<bool> TillCodeExistsAsync(
        Guid tenantId,
        Guid outletId,
        string tillCode,
        Guid? excludeTillId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Tills
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.OutletId == outletId &&
                     x.TillCode == tillCode &&
                     (!excludeTillId.HasValue || x.Id != excludeTillId.Value),
                cancellationToken);
    }

    public async Task<TillListResponse> ListAsync(
        Guid tenantId,
        Guid? outletId,
        int pageNumber,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = from till in _dbContext.Tills.AsNoTracking()
                    join outlet in _dbContext.Outlets.AsNoTracking() on till.OutletId equals outlet.Id
                    where till.TenantId == tenantId &&
                          outlet.TenantId == tenantId &&
                          till.Status != TillConstants.DeletedStatus
                    select new { till, outlet };

        if (outletId.HasValue)
        {
            query = query.Where(x => x.till.OutletId == outletId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(x => x.till.Name.ToUpper().Contains(term) ||
                                     x.till.TillCode.ToUpper().Contains(term) ||
                                     x.outlet.Name.ToUpper().Contains(term) ||
                                     x.outlet.OutletCode.ToUpper().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.outlet.OutletCode)
            .ThenBy(x => x.till.TillCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TillSummaryResponse(
                x.till.Id,
                x.till.OutletId!.Value,
                x.outlet.OutletCode,
                x.outlet.Name,
                x.till.TillCode,
                x.till.Name,
                x.till.Status,
                _dbContext.TillDeviceAssignments.Any(assignment => assignment.TillId == x.till.Id),
                x.till.CreatedAt,
                x.till.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new TillListResponse(items, pageNumber, pageSize, totalCount);
    }

    public async Task<TillResponse?> GetByIdAsync(
        Guid tenantId,
        Guid tillId,
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var row = await (from till in _dbContext.Tills.AsNoTracking()
                         join outlet in _dbContext.Outlets.AsNoTracking() on till.OutletId equals outlet.Id
                         where till.TenantId == tenantId &&
                               till.Id == tillId &&
                               outlet.TenantId == tenantId &&
                               (includeDeleted || till.Status != TillConstants.DeletedStatus)
                         select new { till, outlet })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var isDeviceAssigned = await _dbContext.TillDeviceAssignments
            .AsNoTracking()
            .AnyAsync(x => x.TillId == tillId, cancellationToken);

        return new TillResponse(
            row.till.Id,
            row.till.OutletId!.Value,
            row.outlet.OutletCode,
            row.outlet.Name,
            row.till.TillCode,
            row.till.Name,
            row.till.Status,
            isDeviceAssigned,
            row.till.CreatedAt,
            row.till.UpdatedAt);
    }

    public Task<Till?> GetEditableAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken)
    {
        return _dbContext.Tills
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == tillId &&
                     x.Status != TillConstants.DeletedStatus,
                cancellationToken);
    }

    public Task<bool> HasDeviceAssignmentAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken)
    {
        return _dbContext.TillDeviceAssignments
            .AsNoTracking()
            .AnyAsync(
                assignment => assignment.TillId == tillId &&
                              _dbContext.Tills.Any(till => till.Id == assignment.TillId && till.TenantId == tenantId),
                cancellationToken);
    }

    public async Task AddAsync(Till till, CancellationToken cancellationToken)
    {
        _dbContext.Tills.Add(till);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}