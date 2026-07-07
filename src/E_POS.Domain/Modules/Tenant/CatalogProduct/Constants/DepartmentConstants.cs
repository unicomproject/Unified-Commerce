namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

public static class DepartmentConstants
{
    public const string ActiveStatus = "ACTIVE";
    public const string InactiveStatus = "INACTIVE";
    public const string DeletedStatus = "DELETED";

    public const string ViewPermission = "catalog.departments.view";
    public const string CreatePermission = "catalog.departments.create";
    public const string UpdatePermission = "catalog.departments.update";
    public const string DeletePermission = "catalog.departments.delete";
    public const string ManagePermission = "catalog.departments.manage";

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    public static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();

    public static bool IsValidWriteStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or InactiveStatus;
    }
}
