namespace E_POS.Domain.Modules.AccessControl.Constants;

public static class TenantUserConstants
{
    public const string StatusInvited = "INVITED";
    public const string StatusActive = "ACTIVE";
    public const string StatusInactive = "INACTIVE";
    public const string PendingInvitePasswordHash = "PENDING_INVITE:UNSET";
    public const string DefaultTenantAdminRoleCode = "TENANT_ADMIN";
}
