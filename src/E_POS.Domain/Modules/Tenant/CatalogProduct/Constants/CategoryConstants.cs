namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

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

    public const int MaxCodeLength = 80;
    public const int MaxNameLength = 150;
    public const int MaxSlugLength = 180;
    public const int MaxDescriptionLength = 2000;
    public const int MaxHierarchyDepth = 5;
    public const int RootLevel = 1;
    public const int MaxPageSize = 100;

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    public static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();
    public static string NormalizeName(string name) => name.Trim();
    public static string NormalizeNameForComparison(string name) => name.Trim().ToLowerInvariant();
    public static string NormalizeSlug(string slug) => slug.Trim().ToLowerInvariant();

    public static bool IsValidWriteStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or InactiveStatus;
    }

    public static bool IsValidManagementFilterStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or InactiveStatus;
    }
}
