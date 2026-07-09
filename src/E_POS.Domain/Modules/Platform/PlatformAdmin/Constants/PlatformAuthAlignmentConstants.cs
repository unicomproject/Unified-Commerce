namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

/// <summary>
/// Second Brain alignment constants for platform auth tables (Phase 8).
/// Values are catalog/status codes only — never business identifiers or credentials.
/// </summary>
public static class PlatformAuthAlignmentConstants
{
    public static class AuthenticationMethod
    {
        public const string Password = "PASSWORD";
    }

    public static class FailureReason
    {
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string UserLocked = "USER_LOCKED";
        public const string UserInactive = "USER_INACTIVE";
        public const string PlatformAccessDenied = "PLATFORM_ACCESS_DENIED";
    }

    public static class RevokeReason
    {
        public const string Logout = "LOGOUT";
        public const string RefreshTokenReuse = "REFRESH_TOKEN_REUSE";
    }
}
