namespace E_POS.Application.Modules.Tenant.PricingTax.Dtos;

public record PriceListSummaryResponse(
    Guid Id,
    string PriceListCode,
    string PriceListName,
    string PriceListType,
    string CurrencyCode,
    bool PriceIncludesTax,
    bool IsDefaultPriceList,
    int Priority,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

