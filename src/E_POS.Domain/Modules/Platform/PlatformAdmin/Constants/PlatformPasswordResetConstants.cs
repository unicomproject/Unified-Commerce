namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

public static class PlatformPasswordResetConstants
{
    public const int DefaultLifetimeHours = 1;
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    public const string DeliveryModeAdminSecureLink = "admin_secure_link";
    public const string DeliveryModePendingEmail = "pending_email";

    public static class TokenStatus
    {
        public const string Pending = "PENDING";
        public const string Used = "USED";
        public const string Expired = "EXPIRED";
        public const string Revoked = "REVOKED";
        public const string Invalid = "INVALID";
    }

    public static class AuditMethod
    {
        public const string PasswordResetRequested = "PLATFORM_USER_PASSWORD_RESET_REQUESTED";
        public const string PasswordResetCompleted = "PLATFORM_USER_PASSWORD_RESET_COMPLETED";
        public const string PasswordResetFailed = "PLATFORM_USER_PASSWORD_RESET_FAILED";
        public const string SessionsRevoked = "PLATFORM_USER_SESSIONS_REVOKED";
    }

    public static class AuditStatus
    {
        public const string Success = "SUCCESS";
        public const string Failed = "FAILED";
    }
}
