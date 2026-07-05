namespace E_POS.Application.Modules.PricingTax.Dtos;

public record PriceListItemUpdateRequest(
    decimal SellingPrice,
    decimal? CompareAtPrice,
    decimal MinQuantity,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    string Status);
