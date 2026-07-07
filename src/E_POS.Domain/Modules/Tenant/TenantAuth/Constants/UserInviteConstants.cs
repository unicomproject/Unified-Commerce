namespace E_POS.Domain.Modules.Tenant.TenantAuth.Constants;

public static class UserInviteConstants
{
    public const string StatusPending = "PENDING";
    public const string StatusAccepted = "ACCEPTED";
    public const string StatusExpired = "EXPIRED";
    public const string StatusRevoked = "REVOKED";

    public const string PendingInviteTokenHash = "PENDING_INVITE:UNSET";
}

