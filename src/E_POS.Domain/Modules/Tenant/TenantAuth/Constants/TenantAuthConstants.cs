namespace E_POS.Domain.Modules.Tenant.TenantAuth.Constants;

public static class TenantAuthConstants
{
    public const string ActiveUserStatus = "ACTIVE";
    public const string LockedUserStatus = "LOCKED";
    public const string ActiveTenantStatus = "active";
    public const string SuccessLoginResult = "SUCCESS";
    public const string FailedLoginResult = "FAILED";
    public const string LockedLoginResult = "LOCKED";
    public const string ActiveTokenStatus = "ACTIVE";
    public const string RevokedTokenStatus = "REVOKED";
    public const string IdentityType = "tenant_user";

    /// <summary>
    /// Only <see cref="ActiveTenantStatus"/> is an approved login lifecycle state.
    /// Legacy values such as setup_pending must not remain approved login states.
    /// </summary>
    public static bool IsTenantLoginStatusAllowed(string tenantStatus)
    {
        return string.Equals(tenantStatus, ActiveTenantStatus, StringComparison.OrdinalIgnoreCase);
    }
}
