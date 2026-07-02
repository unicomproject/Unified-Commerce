namespace E_POS.Domain.Modules.PlatformAdministration.Constants;

public static class PlatformUserConstants
{
    /// <summary>
    /// Sentinel password hash for invited users without a configured password yet.
    /// Login rejects empty/invite hashes until password setup is implemented.
    /// </summary>
    public const string PendingInvitePasswordHash = "PENDING_INVITE:UNSET";
}
