namespace E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

public static class PlatformUserProtection
{
    public static bool IsPendingInvite(Entities.PlatformUser user)
    {
        return IsPendingInvitePasswordHash(user.PasswordHash);
    }

    public static bool IsPendingInvitePasswordHash(string passwordHash)
    {
        return string.Equals(
            passwordHash,
            PlatformUserConstants.PendingInvitePasswordHash,
            StringComparison.Ordinal);
    }
}

