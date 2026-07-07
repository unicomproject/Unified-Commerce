namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

public static class CollectionConstants
{
    public const string ActiveStatus = "ACTIVE";
    public const string InactiveStatus = "INACTIVE";
    public const string DeletedStatus = "DELETED";

    public const string ViewPermission = "catalog.collections.view";
    public const string CreatePermission = "catalog.collections.create";
    public const string UpdatePermission = "catalog.collections.update";
    public const string DeletePermission = "catalog.collections.delete";
    public const string ManagePermission = "catalog.collections.manage";

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    public static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();

    public static bool IsValidWriteStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or InactiveStatus;
    }
}

