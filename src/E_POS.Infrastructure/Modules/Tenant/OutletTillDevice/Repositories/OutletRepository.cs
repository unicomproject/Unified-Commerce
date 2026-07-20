using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Modules.Platform.Subscription.Entitlements;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;

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
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.MethodCode)
            .ThenBy(x => x.Id)
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
                outlets = outlets.Where(x => EF.Functions.ILike(x.OutletName, pattern) ||
                                             EF.Functions.ILike(x.OutletCode, pattern));
            }
            else
            {
                var normalizedTerm = term.ToUpperInvariant();
                outlets = outlets.Where(x => x.OutletName.ToUpper().Contains(normalizedTerm) ||
                                             x.OutletCode.ToUpper().Contains(normalizedTerm));
            }
        }

        var activePickupMappings = (
            from mapping in _dbContext.FulfillmentMethodOutlets.AsNoTracking()
            join method in _dbContext.FulfillmentMethods.AsNoTracking()
                on mapping.FulfillmentMethodId equals method.Id
            where mapping.TenantId == tenantId &&
                  method.TenantId == tenantId &&
                  method.MethodType == OutletConstants.PickupMethodType &&
                  method.Status == OutletConstants.ActiveStatus &&
                  mapping.Status == OutletConstants.ActiveStatus
            select new
            {
                Mapping = mapping,
                mapping.OutletId,
                method.IsDefault,
                method.MethodCode,
                MethodId = method.Id
            });

        var query =
            from outlet in outlets
            join activePickupMapping in activePickupMappings
                on outlet.Id equals activePickupMapping.OutletId into pickupJoin
            select new
            {
                Outlet = outlet,
                CollectionEnabled = pickupJoin.Any(),
                PreparationLeadMinutes = pickupJoin
                    .OrderByDescending(x => x.IsDefault)
                    .ThenBy(x => x.MethodCode)
                    .ThenBy(x => x.MethodId)
                    .ThenBy(x => x.Mapping.Id)
                    .Select(x => x.Mapping.PreparationLeadMinutes)
                    .FirstOrDefault(),
                PickupWindowMinutes = pickupJoin
                    .OrderByDescending(x => x.IsDefault)
                    .ThenBy(x => x.MethodCode)
                    .ThenBy(x => x.MethodId)
                    .ThenBy(x => x.Mapping.Id)
                    .Select(x => x.Mapping.PickupWindowMinutes)
                    .FirstOrDefault(),
                CollectionCutoffTime = pickupJoin
                    .OrderByDescending(x => x.IsDefault)
                    .ThenBy(x => x.MethodCode)
                    .ThenBy(x => x.MethodId)
                    .ThenBy(x => x.Mapping.Id)
                    .Select(x => x.Mapping.CutoffTime)
                    .FirstOrDefault()
            };

        var rows = await query
            .OrderBy(x => x.Outlet.OutletCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Outlet.Id,
                x.Outlet.OutletCode,
                x.Outlet.OutletName,
                x.Outlet.Status,
                x.Outlet.OutletType,
                x.Outlet.Timezone,
                x.Outlet.IsDefaultOutlet,
                x.Outlet.Phone,
                x.Outlet.Email,
                x.CollectionEnabled,
                x.PreparationLeadMinutes,
                x.PickupWindowMinutes,
                x.CollectionCutoffTime,
                TotalCount = query.Count()
            })
            .ToListAsync(cancellationToken);

        var totalCount = rows.FirstOrDefault()?.TotalCount ?? (pageNumber == 1 ? 0 : await query.CountAsync(cancellationToken));
        var items = rows
            .Select(x => new OutletSummaryResponse(
                x.Id,
                x.OutletCode,
                x.OutletName,
                x.Status,
                x.OutletType,
                x.Timezone,
                x.IsDefaultOutlet,
                x.Phone,
                x.Email,
                x.CollectionEnabled,
                x.PreparationLeadMinutes,
                x.PickupWindowMinutes,
                x.CollectionCutoffTime))
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
            .Where(x => x.TenantId == tenantId &&
                        x.OutletId == outletId &&
                        x.AddressType == OutletConstants.PhysicalAddressType)
            .Select(x => new OutletAddressResponse(
                x.Id,
                x.AddressType,
                x.AddressLine1,
                x.AddressLine2,
                x.City,
                x.StateOrProvince,
                x.PostalCode,
                x.CountryCode,
                x.ContactName,
                x.ContactPhone,
                x.IsPrimary,
                x.Status))
            .FirstOrDefaultAsync(cancellationToken);

        if (address is null)
        {
            return null;
        }

        var businessHours = await _dbContext.OutletBusinessHours
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.OutletId == outletId)
            .OrderBy(x => x.DayOfWeek)
            .Select(x => new OutletBusinessHourResponse(
                x.Id,
                x.DayOfWeek,
                x.OpeningTime,
                x.ClosingTime,
                x.IsClosed,
                x.ValidFrom,
                x.ValidUntil))
            .ToListAsync(cancellationToken);

        var pickupMapping = await (
            from mapping in _dbContext.FulfillmentMethodOutlets.AsNoTracking()
            join method in _dbContext.FulfillmentMethods.AsNoTracking()
                on mapping.FulfillmentMethodId equals method.Id
            where mapping.TenantId == tenantId &&
                  mapping.OutletId == outletId &&
                  method.TenantId == tenantId &&
                  method.MethodType == OutletConstants.PickupMethodType &&
                  method.Status == OutletConstants.ActiveStatus
            orderby mapping.Status == OutletConstants.ActiveStatus descending,
                method.IsDefault descending,
                method.MethodCode,
                method.Id,
                mapping.Id
            select new
            {
                MappingStatus = mapping.Status,
                MethodStatus = method.Status,
                mapping.PreparationLeadMinutes,
                mapping.PickupWindowMinutes,
                CollectionCutoffTime = mapping.CutoffTime
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new OutletResponse(
            outlet.Id,
            outlet.OutletCode,
            outlet.OutletName,
            outlet.Status,
            outlet.OutletType,
            outlet.Timezone,
            outlet.IsDefaultOutlet,
            outlet.Phone,
            outlet.Email,
            address,
            businessHours,
            pickupMapping is not null &&
            pickupMapping.MappingStatus == OutletConstants.ActiveStatus &&
            pickupMapping.MethodStatus == OutletConstants.ActiveStatus,
            pickupMapping?.PreparationLeadMinutes,
            pickupMapping?.PickupWindowMinutes,
            pickupMapping?.CollectionCutoffTime,
            outlet.CreatedAt,
            outlet.CreatedByTenantUserId,
            outlet.UpdatedAt,
            outlet.UpdatedByTenantUserId);
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
                x => x.TenantId == tenantId &&
                     x.OutletId == outletId &&
                     x.AddressType == OutletConstants.PhysicalAddressType,
                cancellationToken);

        var hours = await _dbContext.OutletBusinessHours
            .Where(x => x.TenantId == tenantId && x.OutletId == outletId)
            .ToListAsync(cancellationToken);

        var pickupMapping = await (
                from mapping in _dbContext.FulfillmentMethodOutlets
                join method in _dbContext.FulfillmentMethods
                    on new { mapping.TenantId, Id = mapping.FulfillmentMethodId }
                    equals new { method.TenantId, method.Id }
                where mapping.TenantId == tenantId &&
                      mapping.OutletId == outletId &&
                      method.MethodType == OutletConstants.PickupMethodType &&
                      method.Status == OutletConstants.ActiveStatus
                orderby method.IsDefault descending,
                    method.MethodCode,
                    method.Id,
                    mapping.Id
                select mapping)
            .FirstOrDefaultAsync(cancellationToken);

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

    public Task<string?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return _dbContext.Tenants
            .AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => (string?)x.Status)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsOutletManagementFeatureEnabledAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var featureId = await _dbContext.PlatformFeatures
            .AsNoTracking()
            .Where(x => x.FeatureCode == OutletConstants.ManagementFeatureCode && x.Status == OutletConstants.ActiveStatus)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (featureId is null)
        {
            return true;
        }

        return await _dbContext.TenantFeatureEntitlements
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.PlatformFeatureId == featureId.Value &&
                     x.EntitlementStatus == TenantEntitlementStatusConstants.Enabled,
                cancellationToken);
    }

    public async Task<bool> IsClickCollectFeatureEnabledAsync(
        Guid tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var entitlements = await (
                from entitlement in _dbContext.TenantFeatureEntitlements.AsNoTracking()
                join feature in _dbContext.PlatformFeatures.AsNoTracking()
                    on entitlement.PlatformFeatureId equals feature.Id
                where entitlement.TenantId == tenantId &&
                      feature.FeatureCode == PlatformTenantFeatureCodes.ClickCollect &&
                      feature.Status == SubscriptionCatalogConstants.RecordStatus.Active
                select new
                {
                    entitlement.EntitlementStatus,
                    entitlement.IsEnabled,
                    entitlement.RevokedAt,
                    entitlement.EffectiveFrom,
                    entitlement.EffectiveUntil
                })
            .ToListAsync(cancellationToken);

        return entitlements.Any(x => TenantEntitlementEffectivePredicate.IsEnabled(
            x.EntitlementStatus,
            x.IsEnabled,
            x.RevokedAt,
            x.EffectiveFrom,
            x.EffectiveUntil,
            now));
    }

    public async Task<OutletCreateOptionsResponse> GetCreateOptionsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenantDefaults = await _dbContext.Tenants
            .AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => new { x.DefaultTimezone })
            .FirstOrDefaultAsync(cancellationToken);

        var tenantCountryCode = await (
            from address in _dbContext.TenantAddresses.AsNoTracking()
            where address.TenantId == tenantId && address.IsPrimary
            select address.CountryCode)
            .FirstOrDefaultAsync(cancellationToken);

        var outletTypes = new[]
        {
            new OutletLookupOptionResponse(OutletConstants.StoreOutletType, "Store"),
            new OutletLookupOptionResponse(OutletConstants.WarehouseOutletType, "Warehouse")
        };

        var countries = TenantCreateWizardReferenceData.CountryCodes
            .Select(item => new OutletCountryOptionResponse(item.Code, item.Name))
            .ToList();

        var timezones = TenantCreateWizardReferenceData.Timezones
            .Select(item => new OutletLookupOptionResponse(item.Value, item.Label))
            .ToList();

        var defaultTimezone = string.IsNullOrWhiteSpace(tenantDefaults?.DefaultTimezone)
            ? OutletConstants.DefaultTimezone
            : tenantDefaults.DefaultTimezone.Trim();

        var defaultCountryCode = string.IsNullOrWhiteSpace(tenantCountryCode)
            ? countries[0].Code
            : tenantCountryCode.Trim().ToUpperInvariant();

        if (countries.All(country => !string.Equals(country.Code, defaultCountryCode, StringComparison.OrdinalIgnoreCase)))
        {
            defaultCountryCode = countries[0].Code;
        }

        if (timezones.All(timezone => !string.Equals(timezone.Value, defaultTimezone, StringComparison.OrdinalIgnoreCase)))
        {
            defaultTimezone = OutletConstants.DefaultTimezone;
        }

        return new OutletCreateOptionsResponse(
            outletTypes,
            countries,
            timezones,
            new OutletCreateDefaultsResponse(defaultCountryCode, defaultTimezone, OutletConstants.ActiveStatus));
    }

    public async Task<bool> AddAsync(
        Outlet outlet,
        OutletAddress address,
        IReadOnlyCollection<OutletBusinessHour> businessHours,
        FulfillmentMethodOutlet? pickupMapping,
        CancellationToken cancellationToken)
    {
        var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        if (outlet.IsDefaultOutlet)
        {
            await ClearOtherDefaultOutletsAsync(outlet.TenantId, outlet.Id, outlet.CreatedAt, cancellationToken);
        }

        _dbContext.Outlets.Add(outlet);
        _dbContext.OutletAddresses.Add(address);
        _dbContext.OutletBusinessHours.AddRange(businessHours);

        if (pickupMapping is not null)
        {
            _dbContext.FulfillmentMethodOutlets.Add(pickupMapping);
        }

        var saved = await SaveChangesHandlingUniqueViolationAsync(cancellationToken);
        if (saved && transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        else if (!saved && transaction is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
        }

        if (transaction is not null)
        {
            await transaction.DisposeAsync();
        }

        return saved;
    }

    public async Task<bool> SaveUpdatedAsync(
        OutletEditAggregate aggregate,
        OutletAddress address,
        IReadOnlyCollection<OutletBusinessHour> businessHours,
        FulfillmentMethodOutlet? newPickupMapping,
        CancellationToken cancellationToken)
    {
        var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        if (aggregate.Outlet.IsDefaultOutlet)
        {
            await ClearOtherDefaultOutletsAsync(
                aggregate.Outlet.TenantId,
                aggregate.Outlet.Id,
                aggregate.Outlet.UpdatedAt ?? aggregate.Outlet.CreatedAt,
                cancellationToken);
        }

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

        var saved = await SaveChangesHandlingUniqueViolationAsync(cancellationToken);
        if (saved && transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        else if (!saved && transaction is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
        }

        if (transaction is not null)
        {
            await transaction.DisposeAsync();
        }

        return saved;
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

    private async Task ClearOtherDefaultOutletsAsync(Guid tenantId, Guid outletId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var otherDefaults = await _dbContext.Outlets
            .Where(x => x.TenantId == tenantId &&
                        x.Id != outletId &&
                        x.IsDefaultOutlet &&
                        x.Status != OutletConstants.DeletedStatus)
            .ToListAsync(cancellationToken);

        foreach (var otherDefault in otherDefaults)
        {
            otherDefault.UpdateProfile(
                otherDefault.OutletName,
                otherDefault.OutletCode,
                otherDefault.Status,
                otherDefault.OutletType,
                otherDefault.Timezone,
                isDefaultOutlet: false,
                otherDefault.Phone,
                otherDefault.Email,
                otherDefault.UpdatedByTenantUserId,
                now);
        }
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



