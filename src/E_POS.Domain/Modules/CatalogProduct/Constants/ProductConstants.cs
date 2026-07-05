namespace E_POS.Domain.Modules.CatalogProduct.Constants;

public static class ProductConstants
{
    public const string ActiveStatus = "ACTIVE";
    public const string InactiveStatus = "INACTIVE";
    public const string DeletedStatus = "DELETED";

    public const string ViewPermission = "catalog.products.view";
    public const string CreatePermission = "catalog.products.create";
    public const string UpdatePermission = "catalog.products.update";
    public const string DeletePermission = "catalog.products.delete";
    public const string ManagePermission = "catalog.products.manage";

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    public static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();

    public static bool IsValidWriteStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or InactiveStatus;
    }
}
