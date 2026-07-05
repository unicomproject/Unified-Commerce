namespace E_POS.Application.Modules.PricingTax.Dtos;

public record PriceListItemResponse(
    Guid Id,
    Guid PriceListId,
    Guid ProductId,
    Guid? ProductVariantId,
    Guid? UomId,
    decimal SellingPrice,
    decimal? CompareAtPrice,
    decimal MinQuantity,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
