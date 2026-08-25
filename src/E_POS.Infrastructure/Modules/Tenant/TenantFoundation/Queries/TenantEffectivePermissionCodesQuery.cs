using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Queries;

internal static class TenantEffectivePermissionCodesQuery
{
    public static IQueryable<string> Build(
        EPosDbContext dbContext,
        Guid tenantUserId,
        Guid tenantId)
    {
        var directPermissions =
            from userPermission in dbContext.TenantUserPermissions.AsNoTracking()
            join permission in dbContext.PermissionDefinitions.AsNoTracking()
                on userPermission.PermissionDefinitionId equals permission.Id
            where userPermission.TenantId == tenantId &&
                  userPermission.TenantUserId == tenantUserId &&
                  userPermission.RevokedAt == null &&
                  permission.IsActive
            select permission.PermissionCode;

        var rolePermissions =
            from userRole in dbContext.TenantUserRoles.AsNoTracking()
            join role in dbContext.TenantRoles.AsNoTracking()
                on userRole.TenantRoleId equals role.Id
            join rolePermission in dbContext.TenantRolePermissions.AsNoTracking()
                on role.Id equals rolePermission.TenantRoleId
            join permission in dbContext.PermissionDefinitions.AsNoTracking()
                on rolePermission.PermissionDefinitionId equals permission.Id
            where userRole.TenantId == tenantId &&
                  userRole.TenantUserId == tenantUserId &&
                  userRole.RevokedAt == null &&
                  role.TenantId == tenantId &&
                  role.IsActive &&
                  rolePermission.TenantId == tenantId &&
                  rolePermission.RevokedAt == null &&
                  permission.IsActive
            select permission.PermissionCode;

        var outletRolePermissions =
            from outletRole in dbContext.OutletUserRoles.AsNoTracking()
            join outlet in dbContext.Outlets.AsNoTracking()
                on outletRole.OutletId equals outlet.Id
            join role in dbContext.TenantRoles.AsNoTracking()
                on outletRole.TenantRoleId equals role.Id
            join rolePermission in dbContext.TenantRolePermissions.AsNoTracking()
                on role.Id equals rolePermission.TenantRoleId
            join permission in dbContext.PermissionDefinitions.AsNoTracking()
                on rolePermission.PermissionDefinitionId equals permission.Id
            where outletRole.TenantId == tenantId &&
                  outletRole.TenantUserId == tenantUserId &&
                  outletRole.RevokedAt == null &&
                  outlet.TenantId == tenantId &&
                  outlet.Status.ToUpper() != OutletConstants.DeletedStatus &&
                  outlet.Status.ToUpper() != OutletConstants.InactiveStatus &&
                  role.TenantId == tenantId &&
                  role.IsActive &&
                  rolePermission.TenantId == tenantId &&
                  rolePermission.RevokedAt == null &&
                  permission.IsActive
            select permission.PermissionCode;

        var outletDirectPermissions =
            from outletPermission in dbContext.OutletUserPermissions.AsNoTracking()
            join outlet in dbContext.Outlets.AsNoTracking()
                on outletPermission.OutletId equals outlet.Id
            join permission in dbContext.PermissionDefinitions.AsNoTracking()
                on outletPermission.PermissionDefinitionId equals permission.Id
            where outletPermission.TenantId == tenantId &&
                  outletPermission.TenantUserId == tenantUserId &&
                  outletPermission.RevokedAt == null &&
                  outlet.TenantId == tenantId &&
                  outlet.Status.ToUpper() != OutletConstants.DeletedStatus &&
                  outlet.Status.ToUpper() != OutletConstants.InactiveStatus &&
                  permission.IsActive
            select permission.PermissionCode;

        return directPermissions
            .Union(rolePermissions)
            .Union(outletRolePermissions)
            .Union(outletDirectPermissions)
            .Where(code => code != string.Empty);
    }
}
