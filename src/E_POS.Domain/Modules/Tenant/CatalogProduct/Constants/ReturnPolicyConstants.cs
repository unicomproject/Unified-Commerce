namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

public static class ReturnPolicyConstants
{
    public const string ActiveStatus = "ACTIVE";
    public const string InactiveStatus = "INACTIVE";
    public const string DeletedStatus = "DELETED";

    public const string ViewPermission = "catalog.return_policies.view";
    public const string CreatePermission = "catalog.return_policies.create";
    public const string UpdatePermission = "catalog.return_policies.update";
    public const string DeletePermission = "catalog.return_policies.delete";
    public const string ManagePermission = "catalog.return_policies.manage";

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    public static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();

    public static bool IsValidWriteStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or InactiveStatus;
    }
}
