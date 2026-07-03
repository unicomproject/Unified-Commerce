namespace E_POS.Domain.Modules.CatalogProduct.Constants;

public static class CategoryConstants
{
    public const string ActiveStatus = "ACTIVE";
    public const string InactiveStatus = "INACTIVE";
    public const string DeletedStatus = "DELETED";

    public const string ViewPermission = "catalog.categories.view";
    public const string CreatePermission = "catalog.categories.create";
    public const string UpdatePermission = "catalog.categories.update";
    public const string DeletePermission = "catalog.categories.delete";
    public const string ManagePermission = "catalog.categories.manage";

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    public static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();

    public static bool IsValidWriteStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or InactiveStatus;
    }
}