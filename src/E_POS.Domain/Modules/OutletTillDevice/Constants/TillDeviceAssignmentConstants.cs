namespace E_POS.Domain.Modules.OutletTillDevice.Constants;

public static class TillDeviceAssignmentConstants
{
    public const string ActiveStatus = "ACTIVE";
    public const string RevokedStatus = "REVOKED";

    public static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();

    public static bool IsValidStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or RevokedStatus;
    }
}