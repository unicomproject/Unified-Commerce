using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Infrastructure.Modules.Platform.Subscription.Entitlements;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Repositories;

public sealed class TenantAdminContextRepository : ITenantAdminContextRepository
{
    private readonly EPosDbContext _dbContext;
    private readonly ITenantFeatureEntitlementEvaluator _featureEntitlementEvaluator;

    public TenantAdminContextRepository(
        EPosDbContext dbContext,
        ITenantFeatureEntitlementEvaluator featureEntitlementEvaluator)
    {
        _dbContext = dbContext;
        _featureEntitlementEvaluator = featureEntitlementEvaluator;
    }

    public async Task<TenantAdminContextData?> GetContextDataAsync(
        Guid tenantUserId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // Fetch tenant name and user info together
        var userInfo = await (
            from user in _dbContext.TenantUsers.AsNoTracking()
            join tenant in _dbContext.Tenants.AsNoTracking()
                on user.TenantId equals tenant.Id
            where user.Id == tenantUserId && user.TenantId == tenantId
            select new
            {
                UserId = user.Id,
                FirstName = user.FullName,
                LastName = string.Empty,
                TenantId = tenant.Id,
                TenantName = tenant.DisplayName,
                TenantTimezone = tenant.DefaultTimezone,
                CurrencyCode = tenant.BaseCurrencyCode,
                Locale = tenant.DefaultLocale
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (userInfo is null)
        {
            return null;
        }

        var tenantLogoUrl = await (
                from profile in _dbContext.TenantProfiles.AsNoTracking()
                join media in _dbContext.MediaAssets.AsNoTracking()
                    on new
                    {
                        profile.TenantId,
                        MediaAssetId = profile.LogoMediaAssetId
                    }
                    equals new
                    {
                        media.TenantId,
                        MediaAssetId = (Guid?)media.Id
                    }
                where profile.TenantId == tenantId &&
                      media.Status == "ACTIVE"
                select media.PublicUrl)
            .FirstOrDefaultAsync(cancellationToken);

        // Roles assigned to this user
        var roles = await (
            from userRole in _dbContext.TenantUserRoles.AsNoTracking()
            join role in _dbContext.TenantRoles.AsNoTracking()
                on userRole.TenantRoleId equals role.Id
            where userRole.TenantUserId == tenantUserId
                  && role.TenantId == tenantId
                  && role.IsActive
            select new TenantAdminContextRoleDto(role.Id, role.RoleName))
            .ToListAsync(cancellationToken);

        var assignedOutletIds = await _dbContext.OutletUserRoles
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.TenantUserId == tenantUserId &&
                        x.RevokedAt == null)
            .Select(x => x.OutletId)
            .Union(_dbContext.OutletUserPermissions
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.TenantUserId == tenantUserId &&
                            x.RevokedAt == null)
                .Select(x => x.OutletId))
            .Distinct()
            .ToListAsync(cancellationToken);

        var outletQuery = _dbContext.Outlets
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId &&
                        o.Status.ToUpper() != "DELETED" &&
                        o.Status.ToUpper() != "INACTIVE");

        if (assignedOutletIds.Count > 0)
        {
            outletQuery = outletQuery.Where(o => assignedOutletIds.Contains(o.Id));
        }

        var outlets = await outletQuery
            .Select(o => new TenantAdminContextOutletDto(o.Id, o.OutletName))
            .ToListAsync(cancellationToken);

        var accessibleOutletIds = outlets.Select(x => x.Id).OrderBy(x => x).ToList();

        // Effective permissions: direct + role-based
        var directPermissions =
            from up in _dbContext.TenantUserPermissions.AsNoTracking()
            join pd in _dbContext.PermissionDefinitions.AsNoTracking()
                on up.PermissionDefinitionId equals pd.Id
            where up.TenantUserId == tenantUserId
                  && pd.IsActive
            select pd.PermissionCode;

        var rolePermissions =
            from ur in _dbContext.TenantUserRoles.AsNoTracking()
            join role in _dbContext.TenantRoles.AsNoTracking()
                on ur.TenantRoleId equals role.Id
            join rp in _dbContext.TenantRolePermissions.AsNoTracking()
                on role.Id equals rp.TenantRoleId
            join pd in _dbContext.PermissionDefinitions.AsNoTracking()
                on rp.PermissionDefinitionId equals pd.Id
            where ur.TenantUserId == tenantUserId
                  && role.TenantId == tenantId
                  && role.IsActive
                  && pd.IsActive
            select pd.PermissionCode;

        var outletRolePermissions =
            from outletRole in _dbContext.OutletUserRoles.AsNoTracking()
            join outlet in _dbContext.Outlets.AsNoTracking()
                on outletRole.OutletId equals outlet.Id
            join role in _dbContext.TenantRoles.AsNoTracking()
                on outletRole.TenantRoleId equals role.Id
            join rp in _dbContext.TenantRolePermissions.AsNoTracking()
                on role.Id equals rp.TenantRoleId
            join pd in _dbContext.PermissionDefinitions.AsNoTracking()
                on rp.PermissionDefinitionId equals pd.Id
            where outletRole.TenantUserId == tenantUserId
                  && outlet.TenantId == tenantId
                  && outlet.Status != "DELETED"
                  && outlet.Status != "INACTIVE"
                  && role.TenantId == tenantId
                  && role.IsActive
                  && pd.IsActive
            select pd.PermissionCode;

        var outletDirectPermissions =
            from outletPermission in _dbContext.OutletUserPermissions.AsNoTracking()
            join outlet in _dbContext.Outlets.AsNoTracking()
                on outletPermission.OutletId equals outlet.Id
            join pd in _dbContext.PermissionDefinitions.AsNoTracking()
                on outletPermission.PermissionDefinitionId equals pd.Id
            where outletPermission.TenantUserId == tenantUserId
                  && outlet.TenantId == tenantId
                  && outlet.Status != "DELETED"
                  && outlet.Status != "INACTIVE"
                  && pd.IsActive
            select pd.PermissionCode;

        var permissions = await directPermissions
            .Union(rolePermissions)
            .Union(outletRolePermissions)
            .Union(outletDirectPermissions)
            .Where(x => x != string.Empty)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        // Enabled feature codes from tenant feature entitlements joined to PlatformFeature.
        // Outlet management uses Strategy B via ITenantFeatureEntitlementEvaluator so that:
        // - disabled/expired canonical never falls through to enabled legacy
        // - legacy-only enabled projects as canonical outlet_management
        // - clients never see both canonical and legacy as independently enabled
        var now = DateTimeOffset.UtcNow;
        var entitlementRows = await (
            from ent in _dbContext.TenantFeatureEntitlements.AsNoTracking()
            join feat in _dbContext.PlatformFeatures.AsNoTracking()
                on ent.PlatformFeatureId equals feat.Id
            where ent.TenantId == tenantId
            select new
            {
                ent.EntitlementStatus,
                ent.IsEnabled,
                ent.RevokedAt,
                ent.EffectiveFrom,
                ent.EffectiveUntil,
                feat.FeatureCode
            })
            .ToListAsync(cancellationToken);

        var enabledFeatures = entitlementRows
            .Where(item => TenantEntitlementEffectivePredicate.IsEnabled(
                item.EntitlementStatus,
                item.IsEnabled,
                item.RevokedAt,
                item.EffectiveFrom,
                item.EffectiveUntil,
                now))
            .Select(item => item.FeatureCode)
            .Where(code => !PlatformTenantFeatureCodes.IsOutletManagementFeatureCode(code))
            .Distinct()
            .ToList();

        var outletEntitled = await _featureEntitlementEvaluator.IsEnabledAsync(
            tenantId,
            PlatformTenantFeatureCodes.OutletManagement,
            now,
            cancellationToken);
        if (outletEntitled)
        {
            enabledFeatures.Add(PlatformTenantFeatureCodes.OutletManagement);
        }

        enabledFeatures = enabledFeatures
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Subscription status (most recent active one, or first found)
        var subscriptionStatus = await _dbContext.TenantSubscriptions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => s.SubscriptionStatus)
            .FirstOrDefaultAsync(cancellationToken) ?? "NONE";

        return new TenantAdminContextData(
            TenantId: userInfo.TenantId,
            TenantName: userInfo.TenantName,
            TenantLogoUrl: tenantLogoUrl,
            TenantTimezone: string.IsNullOrWhiteSpace(userInfo.TenantTimezone) ? "UTC" : userInfo.TenantTimezone,
            CurrencyCode: string.IsNullOrWhiteSpace(userInfo.CurrencyCode) ? "LKR" : userInfo.CurrencyCode,
            Locale: string.IsNullOrWhiteSpace(userInfo.Locale) ? "en-LK" : userInfo.Locale,
            UserId: userInfo.UserId,
            FirstName: userInfo.FirstName,
            LastName: userInfo.LastName,
            Roles: roles,
            Outlets: outlets,
            AccessibleOutletIds: accessibleOutletIds,
            EnabledFeatures: enabledFeatures,
            EffectivePermissions: permissions,
            SubscriptionStatus: subscriptionStatus);
    }
}



