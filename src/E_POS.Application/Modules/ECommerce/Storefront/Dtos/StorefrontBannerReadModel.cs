namespace E_POS.Application.Modules.ECommerce.Storefront.Dtos;

public class StorefrontBannerReadModel
{
    public Guid Id { get; set; }
    public string BannerType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
    public int SortOrder { get; set; }
}
