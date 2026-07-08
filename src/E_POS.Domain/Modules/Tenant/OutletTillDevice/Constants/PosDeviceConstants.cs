namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

public static class PosDeviceConstants
{
    public const string ActiveStatus = "ACTIVE";
    public const string InactiveStatus = "INACTIVE";
    public const string DeletedStatus = "DELETED";
    public const string DefaultDeviceType = "TABLET";
    public const string ViewPermission = "tenant.devices.view";
    public const string CreatePermission = "tenant.devices.create";
    public const string UpdatePermission = "tenant.devices.update";
    public const string DeletePermission = "tenant.devices.delete";
    public const string ManagePermission = "tenant.devices.manage";
    public const string CodeSequenceKey = "POS_DEVICE_CODE";
    public const string CodePrefix = "DEV";
    public const int CodePaddingLength = 3;

    public static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();
    public static string NormalizeDeviceCode(string deviceCode) => deviceCode.Trim().ToUpperInvariant();
    public static string NormalizeDeviceType(string deviceType) => deviceType.Trim().ToUpperInvariant();

    public static bool IsValidStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or InactiveStatus or DeletedStatus;
    }

    public static bool IsValidWriteStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or InactiveStatus;
    }
}
