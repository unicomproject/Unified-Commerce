namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

/// <summary>
/// Authoritative validation and length constants for tenant-specific external brand mappings.
/// </summary>
public static class ExternalBrandMappingConstants
{
    public const int ProviderMaxLength = 50;
    public const int ExternalBrandKeyMaxLength = 150;
    public const int ExternalBrandNameMaxLength = 150;

    public const string DefaultMappingSource = "PRODUCT_CONFIRMED";
}
