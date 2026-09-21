namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

/// <summary>
/// Authoritative validation and length constants for tenant-specific external category mappings.
/// </summary>
public static class ExternalCategoryMappingConstants
{
    public const int ProviderMaxLength = 50;
    public const int ExternalCategoryKeyMaxLength = 150;
    public const int ExternalCategoryNameMaxLength = 150;

    public const string DefaultMappingSource = "PRODUCT_CONFIRMED";
}
