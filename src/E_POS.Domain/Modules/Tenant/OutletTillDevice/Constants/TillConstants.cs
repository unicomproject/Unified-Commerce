namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

public static class TillConstants
{
    public const string ActiveStatus = "ACTIVE";
    public const string InactiveStatus = "INACTIVE";
    public const string MaintenanceStatus = "MAINTENANCE";
    public const string DeletedStatus = "DELETED";
    public const string ViewPermission = "tenant.tills.view";
    public const string CreatePermission = "tenant.tills.create";
    public const string UpdatePermission = "tenant.tills.update";
    public const string DeletePermission = "tenant.tills.delete";
    public const string ManagePermission = "tenant.tills.manage";

    public static string NormalizeTillCode(string tillCode) => tillCode.Trim().ToUpperInvariant();
    public static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();

    public static bool IsValidStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or InactiveStatus or MaintenanceStatus or DeletedStatus;
    }

    public static bool IsValidWriteStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or InactiveStatus or MaintenanceStatus;
    }
}
