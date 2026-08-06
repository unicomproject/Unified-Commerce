namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

public static class ProductRecommendationConstants
{
    public const string FrequentlyBoughtTogetherType = "FREQUENTLY_BOUGHT_TOGETHER";
    public const string ActiveStatus = "ACTIVE";
    public const string InactiveStatus = "INACTIVE";
    public const string DeletedStatus = "DELETED";

    public static string NormalizeType(string recommendationType)
    {
        var normalized = recommendationType?.Trim().ToUpperInvariant()
            ?? throw new ArgumentNullException(nameof(recommendationType));
        return normalized == FrequentlyBoughtTogetherType
            ? normalized
            : throw new ArgumentOutOfRangeException(nameof(recommendationType));
    }

    public static string NormalizeStatus(string status)
    {
        var normalized = status?.Trim().ToUpperInvariant()
            ?? throw new ArgumentNullException(nameof(status));
        return normalized is ActiveStatus or InactiveStatus or DeletedStatus
            ? normalized
            : throw new ArgumentOutOfRangeException(nameof(status));
    }
}
