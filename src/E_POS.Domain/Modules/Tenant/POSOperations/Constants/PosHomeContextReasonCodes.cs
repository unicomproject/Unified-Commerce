namespace E_POS.Domain.Modules.Tenant.POSOperations.Constants;

public static class PosHomeContextReasonCodes
{
    public const string UserContextMissing = "USER_CONTEXT_MISSING";
    public const string DeviceContextMissing = "DEVICE_CONTEXT_MISSING";
    public const string DeviceNotTrusted = "DEVICE_NOT_TRUSTED";
    public const string DeviceNotAssignedToTill = "DEVICE_NOT_ASSIGNED_TO_TILL";
    public const string TillNotFound = "TILL_NOT_FOUND";
    public const string TillInactive = "TILL_INACTIVE";
    public const string NoOpenTillSession = "NO_OPEN_TILL_SESSION";
    public const string OutletTimezoneMissing = "OUTLET_TIMEZONE_MISSING";
    public const string PermissionDenied = "PERMISSION_DENIED";
}
