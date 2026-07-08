using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Repositories;

public sealed class TenantAdminContextRepository : ITenantAdminContextRepository
{
    private readonly EPosDbContext _dbContext;

    public TenantAdminContextRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
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
                TenantName = tenant.DisplayName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (userInfo is null)
        {
            return null;
        }

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

        // Outlets accessible to this tenant
        var outlets = await _dbContext.Outlets
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.Status.ToUpper() != "DELETED")
            .Select(o => new TenantAdminContextOutletDto(o.Id, o.OutletName))
            .ToListAsync(cancellationToken);

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

        var permissions = await directPermissions
            .Union(rolePermissions)
            .Where(x => x != string.Empty)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        // Enabled feature codes from tenant feature entitlements joined to PlatformFeature
        var enabledFeatures = await (
            from ent in _dbContext.TenantFeatureEntitlements.AsNoTracking()
            join feat in _dbContext.PlatformFeatures.AsNoTracking()
                on ent.PlatformFeatureId equals feat.Id
            where ent.TenantId == tenantId
                  && ent.EntitlementStatus.ToUpper() == "ACTIVE"
            select feat.FeatureCode)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

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
            UserId: userInfo.UserId,
            FirstName: userInfo.FirstName,
            LastName: userInfo.LastName,
            Roles: roles,
            Outlets: outlets,
            EnabledFeatures: enabledFeatures,
            EffectivePermissions: permissions,
            SubscriptionStatus: subscriptionStatus);
    }
}



