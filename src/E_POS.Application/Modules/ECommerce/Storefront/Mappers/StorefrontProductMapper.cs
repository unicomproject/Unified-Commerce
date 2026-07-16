using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.ECommerce.Storefront.Mappers;

public static class StorefrontProductMapper
{
    public static StorefrontProductReadModel ToBestSellerReadModel(
        Product product,
        ProductRatingSummary? rating,
        decimal? sellingPrice,
        string? primaryImageUrl)
    {
        return new StorefrontProductReadModel
        {
            Id = product.Id,
            Name = product.ProductName,
            Price = sellingPrice ?? 0m,
            ImageUrl = primaryImageUrl ?? string.Empty,
            Rating = rating?.AverageRating ?? 0m,
            ReviewCount = rating?.TotalReviews ?? 0
        };
    }

    public static IEnumerable<StorefrontProductReadModel> ToBestSellerReadModels(
        this IEnumerable<(Product Product, ProductRatingSummary? Rating, decimal? SellingPrice, string? PrimaryImageUrl)> products)
    {
        return products.Select(x => ToBestSellerReadModel(x.Product, x.Rating, x.SellingPrice, x.PrimaryImageUrl));
    }

    public static StorefrontProductListReadModel ToListReadModel(
        Product product,
        decimal? sellingPrice,
        string? primaryImageUrl,
        decimal averageRating,
        int reviewCount,
        bool isInStock)
    {
        return new StorefrontProductListReadModel
        {
            Id = product.Id,
            Name = product.ProductName,
            Slug = product.ProductSlug,
            ShortDescription = product.ShortDescription ?? string.Empty,
            Price = sellingPrice ?? 0m,
            ImageUrl = primaryImageUrl ?? string.Empty,
            Rating = averageRating,
            ReviewCount = reviewCount,
            IsInStock = isInStock,
            Badge = reviewCount > 0 && averageRating >= 4.5m ? "Best Seller" : null
        };
    }

    public static StorefrontProductImageReadModel ToImageReadModel(ProductImage image, string fallbackAltText)
    {
        return new StorefrontProductImageReadModel
        {
            Id = image.Id,
            Url = string.IsNullOrWhiteSpace(image.ImageUrl) ? image.ImageStorageKey : image.ImageUrl,
            AltText = image.AltText ?? fallbackAltText,
            SortOrder = image.SortOrder,
            IsPrimary = image.IsPrimaryImage
        };
    }

    public static StorefrontProductOptionValueReadModel ToOptionValueReadModel(ProductOptionValue optionValue)
    {
        return new StorefrontProductOptionValueReadModel
        {
            Id = optionValue.Id,
            Name = optionValue.ValueName,
            DisplayName = GetOptionDisplayName(optionValue),
            ColorHex = optionValue.ColorHex,
            ImageUrl = optionValue.ImageUrl,
            SortOrder = optionValue.SortOrder
        };
    }

    public static StorefrontProductVariantReadModel ToVariantReadModel(
        ProductVariant variant,
        string? colour,
        string? size,
        decimal price,
        bool isInStock)
    {
        return new StorefrontProductVariantReadModel
        {
            Id = variant.Id,
            Sku = variant.Sku,
            VariantName = variant.VariantName,
            Colour = colour,
            Size = size,
            Price = price,
            IsDefault = variant.IsDefaultVariant,
            IsInStock = isInStock
        };
    }

    public static StorefrontProductDetailReadModel ToDetailReadModel(
        Product product,
        decimal price,
        ProductRatingSummary? rating,
        bool isInStock,
        IReadOnlyList<StorefrontProductImageReadModel> images,
        IReadOnlyList<StorefrontProductOptionValueReadModel> colours,
        IReadOnlyList<StorefrontProductOptionValueReadModel> sizes,
        IReadOnlyList<StorefrontProductVariantReadModel> variants,
        IReadOnlyList<string> highlights,
        string returnInfo)
    {
        var averageRating = rating?.AverageRating ?? 0m;
        var reviewCount = rating?.TotalReviews ?? 0;

        return new StorefrontProductDetailReadModel
        {
            Id = product.Id,
            Name = product.ProductName,
            Slug = product.ProductSlug,
            ShortDescription = product.ShortDescription ?? string.Empty,
            LongDescription = product.LongDescription ?? string.Empty,
            Price = price,
            Rating = averageRating,
            ReviewCount = reviewCount,
            IsInStock = isInStock,
            Badge = reviewCount > 0 && averageRating >= 4.5m ? "Best Seller" : null,
            Images = images,
            Colours = colours,
            Sizes = sizes,
            Variants = variants,
            Highlights = highlights,
            DeliveryInfo = "Free delivery on eligible orders. Click & Collect ready in as little as 30 minutes.",
            ReturnInfo = returnInfo
        };
    }

    public static string? GetVariantOptionValue(
        Guid variantId,
        IReadOnlyDictionary<Guid, List<ProductVariantOptionValue>> variantOptionLinksByVariant,
        IReadOnlyDictionary<Guid, ProductOption> optionById,
        IReadOnlyDictionary<Guid, ProductOptionValue> optionValueById,
        Func<ProductOption, bool> optionPredicate)
    {
        if (!variantOptionLinksByVariant.TryGetValue(variantId, out var links))
        {
            return null;
        }

        foreach (var link in links)
        {
            if (!optionById.TryGetValue(link.ProductOptionId, out var option) ||
                !optionValueById.TryGetValue(link.ProductOptionValueId, out var optionValue) ||
                !optionPredicate(option))
            {
                continue;
            }

            return GetOptionDisplayName(optionValue);
        }

        return null;
    }

    public static string GetOptionDisplayName(ProductOptionValue optionValue)
    {
        return string.IsNullOrWhiteSpace(optionValue.DisplayName)
            ? optionValue.ValueName
            : optionValue.DisplayName;
    }

    public static bool IsColourOption(ProductOption option)
    {
        return ContainsInvariant(option.OptionCode, "COLOUR") ||
               ContainsInvariant(option.OptionCode, "COLOR") ||
               ContainsInvariant(option.OptionName, "COLOUR") ||
               ContainsInvariant(option.OptionName, "COLOR");
    }

    public static bool IsSizeOption(ProductOption option)
    {
        return ContainsInvariant(option.OptionCode, "SIZE") ||
               ContainsInvariant(option.OptionName, "SIZE");
    }

    public static string? FormatAttributeValue(
        string attributeName,
        string? text,
        decimal? number,
        bool? boolean,
        DateOnly? date)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text.Trim();
        }

        if (number.HasValue)
        {
            return $"{attributeName}: {number.Value:G29}";
        }

        if (boolean.HasValue)
        {
            return $"{attributeName}: {(boolean.Value ? "Yes" : "No")}";
        }

        return date.HasValue ? $"{attributeName}: {date.Value:yyyy-MM-dd}" : null;
    }

    public static string BuildReturnInfo(ReturnPolicy? returnPolicy)
    {
        if (returnPolicy is null)
        {
            return "Easy returns within 30 days.";
        }

        if (!string.IsNullOrWhiteSpace(returnPolicy.Description))
        {
            return returnPolicy.Description;
        }

        return returnPolicy.ReturnWindowDays > 0
            ? $"Easy returns within {returnPolicy.ReturnWindowDays} days."
            : "Returns available under the store policy.";
    }

    private static bool ContainsInvariant(string value, string expected)
    {
        return value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}