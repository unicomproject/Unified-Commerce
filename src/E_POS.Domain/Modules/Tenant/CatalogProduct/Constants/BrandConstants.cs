namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

public static class BrandConstants
{
    public const string ActiveStatus = "ACTIVE";
    public const string InactiveStatus = "INACTIVE";
    public const string DeletedStatus = "DELETED";

    public const string ViewPermission = "catalog.brands.view";
    public const string CreatePermission = "catalog.brands.create";
    public const string UpdatePermission = "catalog.brands.update";
    public const string DeletePermission = "catalog.brands.delete";
    public const string ManagePermission = "catalog.brands.manage";

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    public static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();

    public static bool IsValidWriteStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or InactiveStatus;
    }
}

