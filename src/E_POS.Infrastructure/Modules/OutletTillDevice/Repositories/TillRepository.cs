using E_POS.Application.Modules.OutletTillDevice.Contracts;
using E_POS.Application.Modules.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.OutletTillDevice.Constants;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace E_POS.Infrastructure.Modules.OutletTillDevice.Repositories;

public sealed class TillRepository : ITillRepository
{
    private const string UniqueViolationSqlState = "23505";
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
        var assignedTillIds = _dbContext.TillDeviceAssignments
            .AsNoTracking()
            .Select(x => x.TillId)
            .Distinct();

        var query = from till in _dbContext.Tills.AsNoTracking()
                    join outlet in _dbContext.Outlets.AsNoTracking() on till.OutletId equals outlet.Id
                    join assignedTillId in assignedTillIds on till.Id equals assignedTillId into assignmentJoin
                    where till.TenantId == tenantId &&
                          outlet.TenantId == tenantId &&
                          till.Status != TillConstants.DeletedStatus
                    select new
                    {
                        till,
                        outlet,
                        IsDeviceAssigned = assignmentJoin.Any()
                    };

        if (outletId.HasValue)
        {
            query = query.Where(x => x.till.OutletId == outletId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                var pattern = $"%{term}%";
                query = query.Where(x => EF.Functions.ILike(x.till.Name, pattern) ||
                                         EF.Functions.ILike(x.till.TillCode, pattern) ||
                                         EF.Functions.ILike(x.outlet.Name, pattern) ||
                                         EF.Functions.ILike(x.outlet.OutletCode, pattern));
            }
            else
            {
                var normalizedTerm = term.ToUpperInvariant();
                query = query.Where(x => x.till.Name.ToUpper().Contains(normalizedTerm) ||
                                         x.till.TillCode.ToUpper().Contains(normalizedTerm) ||
                                         x.outlet.Name.ToUpper().Contains(normalizedTerm) ||
                                         x.outlet.OutletCode.ToUpper().Contains(normalizedTerm));
            }
        }

        var rows = await query
            .OrderBy(x => x.outlet.OutletCode)
            .ThenBy(x => x.till.TillCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.till.Id,
                OutletId = x.till.OutletId!.Value,
                x.outlet.OutletCode,
                OutletName = x.outlet.Name,
                x.till.TillCode,
                x.till.Name,
                x.till.Status,
                x.IsDeviceAssigned,
                x.till.CreatedAt,
                x.till.UpdatedAt,
                TotalCount = query.Count()
            })
            .ToListAsync(cancellationToken);

        var totalCount = rows.FirstOrDefault()?.TotalCount ?? (pageNumber == 1 ? 0 : await query.CountAsync(cancellationToken));
        var items = rows
            .Select(x => new TillSummaryResponse(
                x.Id,
                x.OutletId,
                x.OutletCode,
                x.OutletName,
                x.TillCode,
                x.Name,
                x.Status,
                x.IsDeviceAssigned,
                x.CreatedAt,
                x.UpdatedAt))
            .ToList();

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

        var isDeviceAssigned = await HasDeviceAssignmentAsync(tenantId, tillId, cancellationToken);

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
        return (
            from assignment in _dbContext.TillDeviceAssignments.AsNoTracking()
            join till in _dbContext.Tills.AsNoTracking()
                on assignment.TillId equals till.Id
            where assignment.TillId == tillId &&
                  till.TenantId == tenantId
            select assignment.Id)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> AddAsync(Till till, CancellationToken cancellationToken)
    {
        _dbContext.Tills.Add(till);
        return await SaveChangesHandlingUniqueViolationAsync(cancellationToken);
    }

    public Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return SaveChangesHandlingUniqueViolationAsync(cancellationToken);
    }

    private async Task<bool> SaveChangesHandlingUniqueViolationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: UniqueViolationSqlState })
        {
            DetachChangedEntries();
            return false;
        }
    }

    private void DetachChangedEntries()
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries().Where(entry => entry.State is not EntityState.Unchanged and not EntityState.Detached))
        {
            entry.State = EntityState.Detached;
        }
    }

}
