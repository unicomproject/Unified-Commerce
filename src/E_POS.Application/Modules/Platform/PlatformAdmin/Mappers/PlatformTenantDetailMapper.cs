using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Mappers;

public static class PlatformTenantDetailMapper
{
    public static PlatformTenantDetailResponse ApplyActionFlags(
        PlatformTenantDetailResponse detail,
        IReadOnlySet<string> userPermissions)
    {
        var canUpdate = userPermissions.Contains(PlatformPermissionCodes.TenantsUpdate);
        var canActivate = userPermissions.Contains(PlatformPermissionCodes.TenantsActivate)
            && TenantLifecycleRules.CanActivate(detail.Status);
        var canSuspend = userPermissions.Contains(PlatformPermissionCodes.TenantsSuspend)
            && TenantLifecycleRules.CanSuspend(detail.Status, detail.Subscription?.SubscriptionStatus);
        var canManageEntitlements = userPermissions.Contains(PlatformPermissionCodes.TenantsEntitlementsUpdate);

        return detail with
        {
            CanUpdate = canUpdate,
            CanActivate = canActivate,
            CanSuspend = canSuspend,
            CanManageEntitlements = canManageEntitlements
        };
    }
}


