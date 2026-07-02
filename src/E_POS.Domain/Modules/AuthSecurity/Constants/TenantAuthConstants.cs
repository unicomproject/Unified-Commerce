namespace E_POS.Domain.Modules.AuthSecurity.Constants;

public static class TenantAuthConstants
{
    public const string ActiveUserStatus = "ACTIVE";
    public const string LockedUserStatus = "LOCKED";
    public const string ActiveTenantStatus = "active";
    public const string SetupPendingTenantStatus = "setup_pending";
    public const string SuccessLoginResult = "SUCCESS";
    public const string FailedLoginResult = "FAILED";
    public const string LockedLoginResult = "LOCKED";
    public const string ActiveTokenStatus = "ACTIVE";
    public const string RevokedTokenStatus = "REVOKED";
    public const string IdentityType = "tenant_user";

    public static bool IsTenantLoginStatusAllowed(string tenantStatus)
    {
        return string.Equals(tenantStatus, ActiveTenantStatus, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(tenantStatus, SetupPendingTenantStatus, StringComparison.OrdinalIgnoreCase);
    }
}