namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

public static class OutletConstants
{
    public const string ActiveStatus = "ACTIVE";
    public const string InactiveStatus = "INACTIVE";
    public const string DeletedStatus = "DELETED";
    public const string StoreOutletType = "STORE";
    public const string WarehouseOutletType = "WAREHOUSE";
    public const string PhysicalAddressType = "PHYSICAL";
    public const string PickupMethodType = "PICKUP";
    public const string ViewPermission = "tenant.outlets.view";
    public const string ManagePermission = "tenant.outlets.manage";

    public static string NormalizeOutletCode(string outletCode) => outletCode.Trim().ToUpperInvariant();
    public static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();
    public static string NormalizeOutletType(string outletType) => outletType.Trim().ToUpperInvariant();

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

    public static bool IsValidOutletType(string outletType)
    {
        var normalized = NormalizeOutletType(outletType);
        return normalized is StoreOutletType or WarehouseOutletType;
    }
}
