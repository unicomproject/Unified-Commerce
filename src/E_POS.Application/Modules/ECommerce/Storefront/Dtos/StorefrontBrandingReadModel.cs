namespace E_POS.Application.Modules.ECommerce.Storefront.Dtos;

public sealed class StorefrontBrandingReadModel
{
    public Guid TenantId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? StoreDescription { get; set; }
    public string? LogoImageUrl { get; set; }
    public string? FaviconImageUrl { get; set; }
    public string PrimaryColor { get; set; } = "#FF6A00";
    public string SecondaryColor { get; set; } = "#000000";
}
