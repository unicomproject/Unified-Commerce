namespace E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

public static class TillConstants
{
    public const string ActiveStatus = "ACTIVE";
    public const string InactiveStatus = "INACTIVE";
    public const string DeletedStatus = "DELETED";
    public const string StandardTillType = "STANDARD";
    public const string DefaultCurrencyCode = "LKR";
    public const string ViewPermission = "tenant.tills.view";
    public const string CreatePermission = "tenant.tills.create";
    public const string UpdatePermission = "tenant.tills.update";
    public const string DeletePermission = "tenant.tills.delete";
    public const string ManagePermission = "tenant.tills.manage";

    public static string NormalizeTillCode(string tillCode) => tillCode.Trim().ToUpperInvariant();
    public static string NormalizeAreaName(string areaName) => areaName.Trim();
    public static string NormalizeTillType(string tillType) => tillType.Trim().ToUpperInvariant();
    public static string NormalizeCurrencyCode(string currencyCode) => currencyCode.Trim().ToUpperInvariant();
    public static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();

    public static string BuildTillName(string areaName, int tillNumber) =>
        $"{NormalizeAreaName(areaName)} Till {tillNumber:D2}";

    public static string BuildDisplayLabel(string areaName, int tillNumber, string sessionStatus) =>
        $"{BuildTillName(areaName, tillNumber)} / {sessionStatus}";

    public static string FormatSessionStatusLabel(string? sessionStatus, bool isOpen)
    {
        if (!isOpen)
        {
            return "Closed";
        }

        if (string.IsNullOrWhiteSpace(sessionStatus))
        {
            return "Open";
        }

        var normalized = sessionStatus.Trim().ToUpperInvariant();
        return normalized switch
        {
            "OPEN" => "Open",
            "CLOSED" => "Closed",
            _ => char.ToUpperInvariant(normalized[0]) + normalized[1..].ToLowerInvariant()
        };
    }

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
