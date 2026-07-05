namespace E_POS.Application.Modules.PricingTax.Dtos;

public record PriceListCreateRequest(
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
    Guid[]? AssignedOutletIds,
    Guid[]? AssignedSalesChannelIds);
