namespace E_POS.Domain.Modules.PlatformAdministration.Constants;

public static class PlatformRoleProtection
{
    public static bool IsProtectedRole(string roleCode)
    {
        return string.Equals(
            roleCode,
            PlatformRoleCodes.SuperAdministrator,
            StringComparison.Ordinal);
    }
}
