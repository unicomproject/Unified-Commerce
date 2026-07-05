using E_POS.Application.Modules.OutletTillDevice.Contracts;
using E_POS.Application.Modules.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.OutletTillDevice.Constants;
using E_POS.Domain.Modules.OutletTillDevice.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace E_POS.Infrastructure.Modules.OutletTillDevice.Repositories;

public sealed class OutletRepository : IOutletRepository
{
    private const string UniqueViolationSqlState = "23505";
    private readonly EPosDbContext _dbContext;

    public OutletRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public Task<bool> OutletCodeExistsAsync(
        Guid tenantId,
        string outletCode,
        Guid? excludeOutletId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Outlets
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.OutletCode == outletCode &&
                     (!excludeOutletId.HasValue || x.Id != excludeOutletId.Value),
                cancellationToken);
    }

    public async Task<bool> AllOutletsBelongToTenantAsync(
        Guid tenantId,
        Guid[] outletIds,
        CancellationToken cancellationToken)
    {
        var count = await _dbContext.Outlets
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && outletIds.Contains(x.Id), cancellationToken);
        return count == outletIds.Length;
    }

    public Task<Guid?> GetActivePickupFulfillmentMethodIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return _dbContext.FulfillmentMethods
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.MethodType == OutletConstants.PickupMethodType &&
                        x.Status == OutletConstants.ActiveStatus)
            .OrderBy(x => x.MethodCode)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OutletListResponse> ListAsync(
        Guid tenantId,
        int pageNumber,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var outlets = _dbContext.Outlets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.Status != OutletConstants.DeletedStatus);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                var pattern = $"%{term}%";
                outlets = outlets.Where(x => EF.Functions.ILike(x.Name, pattern) ||
                                             EF.Functions.ILike(x.OutletCode, pattern));
            }
            else
            {
                var normalizedTerm = term.ToUpperInvariant();
                outlets = outlets.Where(x => x.Name.ToUpper().Contains(normalizedTerm) ||
                                             x.OutletCode.ToUpper().Contains(normalizedTerm));
            }
        }

        var activePickupOutletIds = (
            from mapping in _dbContext.FulfillmentMethodOutlets.AsNoTracking()
            join method in _dbContext.FulfillmentMethods.AsNoTracking()
                on mapping.FulfillmentMethodId equals method.Id
            where method.TenantId == tenantId &&
                  method.MethodType == OutletConstants.PickupMethodType &&
                  method.Status == OutletConstants.ActiveStatus &&
                  mapping.Status == OutletConstants.ActiveStatus
            select mapping.OutletId)
            .Distinct();

        var query =
            from outlet in outlets
            join activePickupOutletId in activePickupOutletIds
                on outlet.Id equals activePickupOutletId into pickupJoin
            select new
            {
                Outlet = outlet,
                CollectionEnabled = pickupJoin.Any()
            };

        var rows = await query
            .OrderBy(x => x.Outlet.OutletCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Outlet.Id,
                x.Outlet.OutletCode,
                x.Outlet.Name,
                x.Outlet.Status,
                x.Outlet.OutletType,
                x.Outlet.IsOnlineVisible,
                x.Outlet.ContactPhone,
                x.Outlet.ContactEmail,
                x.CollectionEnabled,
                TotalCount = query.Count()
            })
            .ToListAsync(cancellationToken);

        var totalCount = rows.FirstOrDefault()?.TotalCount ?? (pageNumber == 1 ? 0 : await query.CountAsync(cancellationToken));
        var items = rows
            .Select(x => new OutletSummaryResponse(
                x.Id,
                x.OutletCode,
                x.Name,
                x.Status,
                x.OutletType,
                x.IsOnlineVisible,
                x.ContactPhone,
                x.ContactEmail,
                x.CollectionEnabled))
            .ToList();

        return new OutletListResponse(items, pageNumber, pageSize, totalCount);
    }

    public async Task<OutletResponse?> GetByIdAsync(
        Guid tenantId,
        Guid outletId,
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var outlet = await _dbContext.Outlets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == outletId &&
                     (includeDeleted || x.Status != OutletConstants.DeletedStatus),
                cancellationToken);

        if (outlet is null)
        {
            return null;
        }

        var address = await _dbContext.OutletAddresses
            .AsNoTracking()
            .Where(x => x.OutletId == outletId &&
                        x.AddressType == OutletConstants.PhysicalAddressType)
            .Select(x => new OutletAddressResponse(
                x.Id,
                x.AddressType,
                x.AddressLine1,
                x.AddressLine2,
                x.City,
                x.StateOrProvince,
                x.PostalCode,
                x.CountryCode))
            .FirstOrDefaultAsync(cancellationToken);

        if (address is null)
        {
            return null;
        }

        var businessHours = await _dbContext.OutletBusinessHours
            .AsNoTracking()
            .Where(x => x.OutletId == outletId)
            .OrderBy(x => x.DayOfWeek)
            .Select(x => new OutletBusinessHourResponse(
                x.Id,
                x.DayOfWeek,
                x.OpenTime,
                x.CloseTime))
            .ToListAsync(cancellationToken);

        return new OutletResponse(
            outlet.Id,
            outlet.OutletCode,
            outlet.Name,
            outlet.Status,
            outlet.OutletType,
            outlet.IsOnlineVisible,
            outlet.ContactPhone,
            outlet.ContactEmail,
            address,
            businessHours,
            await IsCollectionEnabledAsync(tenantId, outletId, cancellationToken),
            outlet.CreatedAt,
            outlet.UpdatedAt);
    }

    public async Task<OutletEditAggregate?> GetEditAggregateAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var outlet = await _dbContext.Outlets
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == outletId &&
                     x.Status != OutletConstants.DeletedStatus,
                cancellationToken);

        if (outlet is null)
        {
            return null;
        }

        var address = await _dbContext.OutletAddresses
            .FirstOrDefaultAsync(
                x => x.OutletId == outletId &&
                     x.AddressType == OutletConstants.PhysicalAddressType,
                cancellationToken);

        var hours = await _dbContext.OutletBusinessHours
            .Where(x => x.OutletId == outletId)
            .ToListAsync(cancellationToken);

        var pickupMethodIds = _dbContext.FulfillmentMethods
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.MethodType == OutletConstants.PickupMethodType)
            .Select(x => x.Id);

        var pickupMapping = await _dbContext.FulfillmentMethodOutlets
            .FirstOrDefaultAsync(
                x => x.OutletId == outletId &&
                     pickupMethodIds.Contains(x.FulfillmentMethodId),
                cancellationToken);

        return new OutletEditAggregate(outlet, address, hours, pickupMapping);
    }

    public async Task<bool> HasActiveTillOrDeviceAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var hasActiveTill = await _dbContext.Tills
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.OutletId == outletId &&
                     x.Status == OutletConstants.ActiveStatus,
                cancellationToken);

        if (hasActiveTill)
        {
            return true;
        }

        return await _dbContext.PosDevices
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.OutletId == outletId &&
                     x.Status == OutletConstants.ActiveStatus,
                cancellationToken);
    }

    public async Task<bool> AddAsync(
        Outlet outlet,
        OutletAddress address,
        IReadOnlyCollection<OutletBusinessHour> businessHours,
        FulfillmentMethodOutlet? pickupMapping,
        CancellationToken cancellationToken)
    {
        _dbContext.Outlets.Add(outlet);
        _dbContext.OutletAddresses.Add(address);
        _dbContext.OutletBusinessHours.AddRange(businessHours);

        if (pickupMapping is not null)
        {
            _dbContext.FulfillmentMethodOutlets.Add(pickupMapping);
        }

        return await SaveChangesHandlingUniqueViolationAsync(cancellationToken);
    }

    public async Task<bool> SaveUpdatedAsync(
        OutletEditAggregate aggregate,
        OutletAddress address,
        IReadOnlyCollection<OutletBusinessHour> businessHours,
        FulfillmentMethodOutlet? newPickupMapping,
        CancellationToken cancellationToken)
    {
        if (aggregate.PhysicalAddress is null)
        {
            _dbContext.OutletAddresses.Add(address);
        }

        _dbContext.OutletBusinessHours.RemoveRange(aggregate.BusinessHours);
        _dbContext.OutletBusinessHours.AddRange(businessHours);

        if (newPickupMapping is not null)
        {
            _dbContext.FulfillmentMethodOutlets.Add(newPickupMapping);
        }

        return await SaveChangesHandlingUniqueViolationAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<bool> IsCollectionEnabledAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        return _dbContext.FulfillmentMethodOutlets
            .AsNoTracking()
            .AnyAsync(
                mapping => mapping.OutletId == outletId &&
                           mapping.Status == OutletConstants.ActiveStatus &&
                           _dbContext.FulfillmentMethods.Any(method =>
                               method.Id == mapping.FulfillmentMethodId &&
                               method.TenantId == tenantId &&
                               method.MethodType == OutletConstants.PickupMethodType &&
                               method.Status == OutletConstants.ActiveStatus),
                cancellationToken);
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
