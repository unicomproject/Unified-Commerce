using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Application.Modules.Shared.Media;
using E_POS.Domain.Modules.ECommerce.Storefront.Entities;

namespace E_POS.Application.Modules.ECommerce.Storefront.Mappers;

public static class StorefrontBannerMapper
{
    public static StorefrontBannerReadModel ToReadModel(this StorefrontBanner banner, string? mediaPublicUrl = null)
    {
        return new StorefrontBannerReadModel
        {
            Id = banner.Id,
            BannerType = banner.BannerType,
            Title = banner.Title,
            Subtitle = banner.Subtitle,
            ImageUrl = MediaUrlResolver.PreferMediaAssetOrEmpty(mediaPublicUrl, banner.ImageUrl),
            ActionText = banner.ActionText,
            ActionUrl = banner.ActionUrl,
            SortOrder = banner.SortOrder
        };
    }

    public static IEnumerable<StorefrontBannerReadModel> ToReadModels(this IEnumerable<StorefrontBanner> banners)
    {
        return banners.Select(banner => banner.ToReadModel());
    }
}
