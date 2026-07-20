namespace E_POS.Application.Modules.ECommerce.Storefront.Dtos;

public class StorefrontProductReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
}

public class StorefrontProductListReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsInStock { get; set; }
    public string? Badge { get; set; }
}

public class StorefrontProductDetailReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsInStock { get; set; }
    public string? Badge { get; set; }
    public IReadOnlyList<StorefrontProductImageReadModel> Images { get; set; } = [];
    public IReadOnlyList<StorefrontProductOptionValueReadModel> Colours { get; set; } = [];
    public IReadOnlyList<StorefrontProductOptionValueReadModel> Sizes { get; set; } = [];
    public IReadOnlyList<StorefrontProductVariantReadModel> Variants { get; set; } = [];
    public IReadOnlyList<string> Highlights { get; set; } = [];
    public string DeliveryInfo { get; set; } = string.Empty;
    public string ReturnInfo { get; set; } = string.Empty;
}

public class StorefrontProductImageReadModel
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class StorefrontProductOptionValueReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
}

public class StorefrontProductVariantReadModel
{
    public Guid Id { get; set; }
    public string? Sku { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public string? Colour { get; set; }
    public string? Size { get; set; }
    public decimal Price { get; set; }
    public bool IsDefault { get; set; }
    public bool IsInStock { get; set; }
}

public class StorefrontPagedReadModel<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class StorefrontCategoryReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class StorefrontCategoryListReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int SortOrder { get; set; }
}

public class StorefrontStoreReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public bool IsOpen { get; set; }
    public int PreparationLeadMinutes { get; set; }
}

public sealed class StorefrontCollectionOptionsReadModel
{
    public Guid OutletId { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public int PreparationLeadMinutes { get; set; }
    public int PickupWindowMinutes { get; set; }
    public TimeOnly? CutoffTime { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public DateTimeOffset EarliestCollectionAt { get; set; }
    public IReadOnlyList<StorefrontCollectionDateReadModel> Dates { get; set; } = [];
}

public sealed class StorefrontCollectionDateReadModel
{
    public DateOnly Date { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public TimeOnly OpeningTime { get; set; }
    public TimeOnly ClosingTime { get; set; }
    public IReadOnlyList<StorefrontCollectionWindowReadModel> Windows { get; set; } = [];
}

public sealed class StorefrontCollectionWindowReadModel
{
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
}

public sealed class StorefrontCollectionConfigurationReadModel
{
    public Guid OutletId { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public int? PreparationLeadMinutes { get; set; }
    public int? PickupWindowMinutes { get; set; }
    public TimeOnly? CutoffTime { get; set; }
    public IReadOnlyList<StorefrontCollectionBusinessHourReadModel> BusinessHours { get; set; } = [];
}

public sealed class StorefrontCollectionBusinessHourReadModel
{
    public short DayOfWeek { get; set; }
    public TimeOnly? OpeningTime { get; set; }
    public TimeOnly? ClosingTime { get; set; }
    public bool IsClosed { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
}

public sealed class StorefrontSearchRequest
{
    public string? SearchText { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Colour { get; set; }
    public string? Size { get; set; }
    public bool? InStock { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class StorefrontSearchReadModel
{
    public StorefrontPagedReadModel<StorefrontProductListReadModel> Products { get; set; } = new();
    public IReadOnlyList<StorefrontSearchMatchReadModel> Categories { get; set; } = [];
    public IReadOnlyList<StorefrontSearchMatchReadModel> Collections { get; set; } = [];
    public int TotalCount => Products.TotalCount + Categories.Count + Collections.Count;
}

public sealed class StorefrontSearchMatchReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
}
