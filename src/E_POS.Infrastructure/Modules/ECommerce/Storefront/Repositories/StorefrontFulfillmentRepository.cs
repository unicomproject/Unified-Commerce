using System.Data;
using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Modules.Platform.Subscription.Entitlements;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;

public sealed class StorefrontFulfillmentRepository : IStorefrontFulfillmentRepository
{
    private static readonly string[] RequiredCollectionFeatures =
    [
        PlatformTenantFeatureCodes.OnlineStore,
        PlatformTenantFeatureCodes.ClickCollect
    ];

    private readonly EPosDbContext _dbContext;

    public StorefrontFulfillmentRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<StorefrontStoreReadModel>> GetAvailableStoresAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!await HasCollectionAccessAsync(tenantId, DateTimeOffset.UtcNow, cancellationToken))
            return [];

        var connection = _dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT o.id, o.outlet_name,
                       a.address_line1, a.city,
                       o.timezone,
                       (SELECT fmo.preparation_lead_minutes
                        FROM fulfillment_method_outlets fmo
                        JOIN fulfillment_methods fm ON fm.tenant_id = fmo.tenant_id AND fm.id = fmo.fulfillment_method_id
                        WHERE fmo.tenant_id = @tenantId AND fmo.outlet_id = o.id
                          AND fmo.status = 'ACTIVE' AND fm.status = 'ACTIVE' AND fm.method_type = 'PICKUP'
                        LIMIT 1) as prep_minutes
                FROM outlets o
                LEFT JOIN outlet_addresses a ON a.tenant_id = @tenantId
                    AND a.outlet_id = o.id
                    AND a.is_primary = true
                    AND a.status = 'ACTIVE'
                WHERE o.tenant_id = @tenantId
                  AND o.status = 'ACTIVE'
                  AND EXISTS (
                      SELECT 1
                      FROM fulfillment_method_outlets fmo
                      JOIN fulfillment_methods fm
                        ON fm.tenant_id = fmo.tenant_id
                       AND fm.id = fmo.fulfillment_method_id
                      WHERE fmo.tenant_id = @tenantId
                        AND fmo.outlet_id = o.id
                        AND fmo.status = 'ACTIVE'
                        AND fm.status = 'ACTIVE'
                        AND fm.method_type = 'PICKUP'
                  )";

            var param = cmd.CreateParameter();
            param.ParameterName = "tenantId";
            param.Value = tenantId;
            cmd.Parameters.Add(param);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var stores = new List<StorefrontStoreReadModel>();
            var storeTimezones = new Dictionary<Guid, string>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var addressLine1 = reader.IsDBNull(2) ? null : reader.GetString(2);
                var city = reader.IsDBNull(3) ? null : reader.GetString(3);
                var id = reader.GetGuid(0);
                var timezone = reader.IsDBNull(4) ? "UTC" : reader.GetString(4);
                var prepMins = reader.IsDBNull(5) ? 30 : reader.GetInt32(5);

                stores.Add(new StorefrontStoreReadModel
                {
                    Id = id,
                    Name = reader.GetString(1),
                    Address = addressLine1 != null ? $"{addressLine1}, {city}" : string.Empty,
                    IsAvailable = true,
                    PreparationLeadMinutes = prepMins
                });
                storeTimezones[id] = timezone;
            }
            await reader.CloseAsync();

            var outletIds = stores.Select(x => x.Id).ToList();
            if (outletIds.Any())
            {
                var businessHours = await _dbContext.OutletBusinessHours.AsNoTracking()
                    .Where(x => x.TenantId == tenantId && outletIds.Contains(x.OutletId) && !x.IsClosed)
                    .ToListAsync(cancellationToken);

                var now = DateTimeOffset.UtcNow;
                foreach (var store in stores)
                {
                    var tzString = storeTimezones.GetValueOrDefault(store.Id, "UTC");
                    TimeZoneInfo tz;
                    try { tz = TimeZoneInfo.FindSystemTimeZoneById(tzString); }
                    catch { tz = TimeZoneInfo.Utc; }

                    var localTime = TimeZoneInfo.ConvertTime(now, tz);
                    var localDate = DateOnly.FromDateTime(localTime.DateTime);
                    var localTimeOnly = TimeOnly.FromDateTime(localTime.DateTime);
                    var dayOfWeek = (short)localDate.DayOfWeek;

                    var todayHours = businessHours
                        .Where(x => x.OutletId == store.Id && x.DayOfWeek == dayOfWeek &&
                                    (!x.ValidFrom.HasValue || x.ValidFrom.Value <= localDate) &&
                                    (!x.ValidUntil.HasValue || x.ValidUntil.Value >= localDate))
                        .OrderByDescending(x => x.ValidFrom.HasValue || x.ValidUntil.HasValue)
                        .FirstOrDefault();

                    store.IsAvailable = true;
                    store.IsOpen = todayHours != null && todayHours.OpeningTime <= localTimeOnly && todayHours.ClosingTime >= localTimeOnly;
                }
            }

            return stores;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task<StorefrontCollectionConfigurationReadModel?> GetCollectionConfigurationAsync(
        Guid tenantId,
        Guid outletId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (!await HasCollectionAccessAsync(tenantId, now, cancellationToken))
            return null;

        var configuration = await (
            from outlet in _dbContext.Outlets.AsNoTracking()
            join mapping in _dbContext.FulfillmentMethodOutlets.AsNoTracking()
                on new { outlet.TenantId, OutletId = outlet.Id }
                equals new { mapping.TenantId, mapping.OutletId }
            join method in _dbContext.FulfillmentMethods.AsNoTracking()
                on new { mapping.TenantId, FulfillmentMethodId = mapping.FulfillmentMethodId }
                equals new { method.TenantId, FulfillmentMethodId = method.Id }
            where outlet.TenantId == tenantId
                  && outlet.Id == outletId
                  && outlet.Status == "ACTIVE"
                  && mapping.TenantId == tenantId
                  && mapping.Status == "ACTIVE"
                  && method.TenantId == tenantId
                  && method.Status == "ACTIVE"
                  && method.MethodType == "PICKUP"
            orderby method.IsDefault descending,
                method.MethodCode,
                method.Id,
                mapping.Id
            select new
            {
                OutletId = outlet.Id,
                outlet.OutletName,
                outlet.Timezone,
                mapping.PreparationLeadMinutes,
                mapping.PickupWindowMinutes,
                mapping.CutoffTime
            }).FirstOrDefaultAsync(cancellationToken);

        if (configuration is null)
            return null;

        var businessHours = await _dbContext.OutletBusinessHours
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.OutletId == outletId)
            .OrderBy(x => x.DayOfWeek)
            .Select(x => new StorefrontCollectionBusinessHourReadModel
            {
                DayOfWeek = x.DayOfWeek,
                OpeningTime = x.OpeningTime,
                ClosingTime = x.ClosingTime,
                IsClosed = x.IsClosed,
                ValidFrom = x.ValidFrom,
                ValidUntil = x.ValidUntil
            })
            .ToListAsync(cancellationToken);

        return new StorefrontCollectionConfigurationReadModel
        {
            OutletId = configuration.OutletId,
            OutletName = configuration.OutletName,
            Timezone = configuration.Timezone,
            PreparationLeadMinutes = configuration.PreparationLeadMinutes,
            PickupWindowMinutes = configuration.PickupWindowMinutes,
            CutoffTime = configuration.CutoffTime,
            BusinessHours = businessHours
        };
    }

    private async Task<bool> HasCollectionAccessAsync(
        Guid tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var tenantAvailable = await _dbContext.Tenants.AsNoTracking().AnyAsync(
            x => x.Id == tenantId && x.Status == TenantStatusConstants.Active,
            cancellationToken);
        if (!tenantAvailable)
            return false;

        var entitlements = await (
                from entitlement in _dbContext.TenantFeatureEntitlements.AsNoTracking()
                join feature in _dbContext.PlatformFeatures.AsNoTracking()
                    on entitlement.PlatformFeatureId equals feature.Id
                where entitlement.TenantId == tenantId &&
                      RequiredCollectionFeatures.Contains(feature.FeatureCode) &&
                      feature.Status == SubscriptionCatalogConstants.RecordStatus.Active
                select new
                {
                    feature.FeatureCode,
                    entitlement.EntitlementStatus,
                    entitlement.IsEnabled,
                    entitlement.RevokedAt,
                    entitlement.EffectiveFrom,
                    entitlement.EffectiveUntil
                })
            .ToListAsync(cancellationToken);

        return RequiredCollectionFeatures.All(requiredFeature =>
            entitlements.Any(x =>
                string.Equals(x.FeatureCode, requiredFeature, StringComparison.OrdinalIgnoreCase) &&
                TenantEntitlementEffectivePredicate.IsEnabled(
                    x.EntitlementStatus,
                    x.IsEnabled,
                    x.RevokedAt,
                    x.EffectiveFrom,
                    x.EffectiveUntil,
                    now)));
    }
}
