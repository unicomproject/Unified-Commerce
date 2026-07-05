namespace E_POS.Domain.Modules.PricingTax.Constants;

public static class PricingTaxConstants
{
    public const string ActiveStatus = "ACTIVE";
    public const string InactiveStatus = "INACTIVE";
    public const string DeletedStatus = "DELETED";

    // Permissions
    public const string ViewPermission = "pricing.price_lists.view";
    public const string CreatePermission = "pricing.price_lists.create";
    public const string UpdatePermission = "pricing.price_lists.update";
    public const string DeletePermission = "pricing.price_lists.delete";
    public const string ManagePermission = "pricing.price_lists.manage";

    // Price List Types
    public const string PosType = "POS";
    public const string StorefrontType = "STOREFRONT";
    public const string CustomType = "CUSTOM";

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    public static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();

    public static bool IsValidWriteStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or InactiveStatus;
    }

    public static bool IsValidPriceListType(string type)
    {
        var normalized = type.Trim().ToUpperInvariant();
        return normalized is PosType or StorefrontType or CustomType;
    }
}
